using FluentAssertions;
using Moq;
using TCS.Core.DTOs.Requests;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Core.Services;
using Xunit;

namespace TCS.Tests.Services;

public class TrainingServiceTests
{
    private static TrainingService BuildSvc(
        ITrainingRepository? trainingRepo = null,
        ILicenseRepository? licenseRepo = null,
        IEmployeeRepository? empRepo = null) =>
        new(trainingRepo ?? Mock.Of<ITrainingRepository>(),
            licenseRepo ?? Mock.Of<ILicenseRepository>(),
            empRepo ?? Mock.Of<IEmployeeRepository>());

    // ── CreateHeader ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateHeader_Hours_FromLicense()
    {
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("1.1", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "1.1", Description = "Test", Hours = 24, Years = 2 });

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "1.1", default)).ReturnsAsync(false);
        trainingRepo.Setup(r => r.AddHeaderAsync(It.IsAny<TrainingHeader>(), default)).Returns(Task.CompletedTask);

        var result = await BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(
            new CreateTrainingHeaderRequest("E001", "1.1", null, null));
        Assert.Equal(24, result.Hours);
        Assert.Equal(2, result.Years);
    }

    [Fact]
    public async Task CreateHeader_AlreadyExists_ThrowsInvalidOperation()
    {
        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "1.1", default)).ReturnsAsync(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BuildSvc(trainingRepo.Object).CreateHeaderAsync(new CreateTrainingHeaderRequest("E001", "1.1", null, null)));
    }

    [Fact]
    public async Task CreateHeader_LicenseNotFound_ThrowsKeyNotFound()
    {
        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "9.9", default)).ReturnsAsync(false);

        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("9.9", default)).ReturnsAsync((LicenseMaster?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(
                new CreateTrainingHeaderRequest("E001", "9.9", null, null)));
    }

    [Fact]
    public async Task CreateHeader_StandaloneCategory_NoChildren_Succeeds()
    {
        // 無小類大類（HasChildLicensesAsync 回 false）、IsOther=false → 成功，Hours/Years 帶主檔
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("5", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "5", Description = "獨立真證照", Hours = 8, Years = 1 });
        licenseRepo.Setup(r => r.HasChildLicensesAsync("5", default)).ReturnsAsync(false);

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "5", default)).ReturnsAsync(false);
        TrainingHeader? added = null;
        trainingRepo.Setup(r => r.AddHeaderAsync(It.IsAny<TrainingHeader>(), default))
            .Callback<TrainingHeader, CancellationToken>((h, _) => added = h)
            .Returns(Task.CompletedTask);

        var result = await BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(
            new CreateTrainingHeaderRequest("E001", "5", null, null));

        result.LicenseType.Should().Be("5");
        result.Hours.Should().Be(8);
        result.Years.Should().Be(1);
        added!.LicenseType.Should().Be("5");
    }

    [Fact]
    public async Task CreateHeader_CategoryWithChildren_ThrowsInvalidOperation()
    {
        // 有小類大類（HasChildLicensesAsync 回 true）→ 丟 InvalidOperationException（含「小類」）
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("1", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "1", Description = "電氣大類" });
        licenseRepo.Setup(r => r.HasChildLicensesAsync("1", default)).ReturnsAsync(true);

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "1", default)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(
                new CreateTrainingHeaderRequest("E001", "1", null, null)));
        ex.Message.Should().Contain("小類");
    }

    [Fact]
    public async Task CreateHeader_ReservedCode99_NonOther_ThrowsInvalidOperation()
    {
        // LicenseType="99"、IsOther=false → 丟 InvalidOperationException（含「其他」）
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("99", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "99", Description = "其他" });

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "99", default)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(
                new CreateTrainingHeaderRequest("E001", "99", null, null)));
        ex.Message.Should().Contain("其他");
    }

    [Fact]
    public async Task CreateHeader_Other_Major_GeneratesNextSequence()
    {
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("99", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "99", Description = "其他", Hours = null, Years = null });

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.GetHeaderLicenseTypesByPrefixAsync("E001", "99", default))
            .ReturnsAsync(new List<string> { "99.1", "99.2" });
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "99.3", default)).ReturnsAsync(false);
        TrainingHeader? added = null;
        trainingRepo.Setup(r => r.AddHeaderAsync(It.IsAny<TrainingHeader>(), default))
            .Callback<TrainingHeader, CancellationToken>((h, _) => added = h)
            .Returns(Task.CompletedTask);

        var req = new CreateTrainingHeaderRequest("E001", "99", "我的自定義證照", null, IsOther: true, Hours: 10, Years: 3);
        var result = await BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(req);

        result.LicenseType.Should().Be("99.3");
        result.Hours.Should().Be(10);
        result.Years.Should().Be(3);
        result.Remark.Should().Be("我的自定義證照");
        added!.LicenseType.Should().Be("99.3");
    }

    [Fact]
    public async Task CreateHeader_Other_Minor_GeneratesTwoDotCode_DefaultsNullHours()
    {
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("1", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "1", Description = "電氣", Hours = null, Years = null });

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.GetHeaderLicenseTypesByPrefixAsync("E001", "1.0", default))
            .ReturnsAsync(new List<string>());
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "1.0.1", default)).ReturnsAsync(false);
        trainingRepo.Setup(r => r.AddHeaderAsync(It.IsAny<TrainingHeader>(), default)).Returns(Task.CompletedTask);

        var req = new CreateTrainingHeaderRequest("E001", "1", "現場自訂", null, IsOther: true);
        var result = await BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(req);

        result.LicenseType.Should().Be("1.0.1");
        result.Hours.Should().BeNull();
        result.Years.Should().BeNull();
    }

    // ── GetHeader ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHeader_NotFound_ThrowsKeyNotFound()
    {
        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.GetHeaderAsync("E001", "9.9", true, default)).ReturnsAsync((TrainingHeader?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            BuildSvc(trainingRepo.Object).GetHeaderAsync("E001", "9.9"));
    }

    // ── UpdateHeader ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateHeader_UpdatesRemark()
    {
        var header = new TrainingHeader
        {
            EmployeeId = "E001", LicenseType = "1.1", Hours = 16,
            Remark = "舊備註",
            Details = new List<TrainingDetail>()
        };
        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.GetHeaderAsync("E001", "1.1", true, default)).ReturnsAsync(header);
        trainingRepo.Setup(r => r.UpdateHeaderAsync(It.IsAny<TrainingHeader>(), default)).Returns(Task.CompletedTask);

        var dto = await BuildSvc(trainingRepo.Object).UpdateHeaderAsync(
            new UpdateTrainingHeaderRequest("E001", "1.1", "新備註", null));
        dto.Remark.Should().Be("新備註");
    }

    // ── AddDetail ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddDetail_FirstRecord_MustBeType1()
    {
        var header = new TrainingHeader
        {
            EmployeeId = "E001", LicenseType = "1.1", Hours = 16,
            Details = new List<TrainingDetail>()
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetHeaderAsync("E001", "1.1", true, default)).ReturnsAsync(header);

        var req = new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)), 2, 8m);
        await Assert.ThrowsAsync<InvalidOperationException>(() => BuildSvc(repoMock.Object).AddDetailAsync(req));
    }

    [Fact]
    public async Task AddDetail_SecondRecordType1_Reacquire_Succeeds()
    {
        var existing = DateTime.Today.AddMonths(-3);
        var header = new TrainingHeader
        {
            EmployeeId = "E001", LicenseType = "1.1", Hours = 16,
            Details = new List<TrainingDetail>
            {
                new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = existing, TrainingType = 1, Hours = 0m }
            }
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetHeaderAsync("E001", "1.1", true, default)).ReturnsAsync(header);
        repoMock.Setup(r => r.AddDetailAsync(It.IsAny<TrainingDetail>(), default)).Returns(Task.CompletedTask);

        // 過期重考：已有紀錄仍可新增「取得證照」（2026-08-07 T4）
        var req = new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(existing.AddMonths(1)), 1, null);
        var dto = await BuildSvc(repoMock.Object).AddDetailAsync(req);
        dto.TrainingType.Should().Be(1);
    }

    [Fact]
    public async Task AddDetail_DuplicateDate_ThrowsInvalidOperation()
    {
        var existingDate = DateTime.Today.AddMonths(-2);
        var header = new TrainingHeader
        {
            EmployeeId = "E001", LicenseType = "1.1", Hours = 16,
            Details = new List<TrainingDetail>
            {
                new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = existingDate, TrainingType = 1, Hours = 4m }
            }
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetHeaderAsync("E001", "1.1", true, default)).ReturnsAsync(header);

        var req = new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(existingDate), 2, 4m);
        await Assert.ThrowsAsync<InvalidOperationException>(() => BuildSvc(repoMock.Object).AddDetailAsync(req));
    }

    [Fact]
    public async Task AddDetail_ValidFirstRecord_ReturnsDto()
    {
        var header = new TrainingHeader
        {
            EmployeeId = "E001", LicenseType = "1.1", Hours = 16,
            Details = new List<TrainingDetail>()
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetHeaderAsync("E001", "1.1", true, default)).ReturnsAsync(header);
        repoMock.Setup(r => r.AddDetailAsync(It.IsAny<TrainingDetail>(), default)).Returns(Task.CompletedTask);

        var today = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));
        var req = new CreateTrainingDetailRequest("E001", "1.1", today, 1, 8m);
        var dto = await BuildSvc(repoMock.Object).AddDetailAsync(req);
        dto.TrainingType.Should().Be(1);
        dto.Hours.Should().Be(8m);
        dto.TrainingDate.Should().Be(today);
    }

    [Fact]
    public async Task AddDetail_RetrainBeforeAcquireDate_ThrowsInvalidOperation()
    {
        var acquireDate = DateTime.Today.AddMonths(-2);
        var header = new TrainingHeader
        {
            EmployeeId = "E001", LicenseType = "1.1", Hours = 16,
            Details = new List<TrainingDetail>
            {
                new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = acquireDate, TrainingType = 1, Hours = 0m }
            }
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetHeaderAsync("E001", "1.1", true, default)).ReturnsAsync(header);

        // 回訓日期早於取得證照（anchor）日期 → 應被拒絕（否則衍生推導會忽略該筆）
        var req = new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(acquireDate.AddMonths(-1)), 2, 4m);
        await Assert.ThrowsAsync<InvalidOperationException>(() => BuildSvc(repoMock.Object).AddDetailAsync(req));
    }

    [Fact]
    public async Task AddDetail_RetrainAfterAcquireDate_Succeeds()
    {
        var acquireDate = DateTime.Today.AddMonths(-3);
        var header = new TrainingHeader
        {
            EmployeeId = "E001", LicenseType = "1.1", Hours = 16,
            Details = new List<TrainingDetail>
            {
                new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = acquireDate, TrainingType = 1, Hours = 0m }
            }
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetHeaderAsync("E001", "1.1", true, default)).ReturnsAsync(header);
        repoMock.Setup(r => r.AddDetailAsync(It.IsAny<TrainingDetail>(), default)).Returns(Task.CompletedTask);

        var req = new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(acquireDate.AddMonths(1)), 2, 4m);
        var dto = await BuildSvc(repoMock.Object).AddDetailAsync(req);
        dto.TrainingType.Should().Be(2);
    }

    [Fact]
    public async Task AddDetail_DateNotAfterLatest_ThrowsInvalidOperation()
    {
        var acquireDate = DateTime.Today.AddMonths(-3);
        var latestRetrain = DateTime.Today.AddMonths(-1);
        var header = new TrainingHeader
        {
            EmployeeId = "E001", LicenseType = "1.1", Hours = 16,
            Details = new List<TrainingDetail>
            {
                new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = acquireDate, TrainingType = 1, Hours = 0m },
                new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = latestRetrain, TrainingType = 2, Hours = 4m }
            }
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetHeaderAsync("E001", "1.1", true, default)).ReturnsAsync(header);

        // 新增日期介於兩筆之間（早於最後一筆）→ 應被拒絕（append-only）
        var req = new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today.AddMonths(-2)), 2, 4m);
        await Assert.ThrowsAsync<InvalidOperationException>(() => BuildSvc(repoMock.Object).AddDetailAsync(req));
    }

    [Fact]
    public async Task AddDetail_HeaderNotFound_ThrowsKeyNotFound()
    {
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetHeaderAsync("E001", "9.9", true, default)).ReturnsAsync((TrainingHeader?)null);

        var req = new CreateTrainingDetailRequest("E001", "9.9", DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)), 1, 8m);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => BuildSvc(repoMock.Object).AddDetailAsync(req));
    }

    // ── UpdateDetail ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDetail_NonFirstRecord_ChangesTrainingType()
    {
        var first = DateTime.Today.AddMonths(-4);
        var date = DateTime.Today.AddMonths(-2);
        var detail = new TrainingDetail
        {
            EmployeeId = "E001", LicenseType = "1.1", TrainingDate = date,
            TrainingType = 2, Hours = 4m
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetDetailAsync("E001", "1.1", date, default)).ReturnsAsync(detail);
        repoMock.Setup(r => r.GetDetailsAsync("E001", "1.1", default)).ReturnsAsync(new List<TrainingDetail>
        {
            new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = first, TrainingType = 1, Hours = 0m },
            detail
        });
        repoMock.Setup(r => r.UpdateDetailAsync(It.IsAny<TrainingDetail>(), default)).Returns(Task.CompletedTask);

        // 非首筆：類型 2 → 1（過期重考補登）
        var req = new UpdateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(date), 1, 6m);
        var dto = await BuildSvc(repoMock.Object).UpdateDetailAsync(req);

        dto.TrainingType.Should().Be(1);
        dto.Hours.Should().Be(6m);
    }

    [Fact]
    public async Task UpdateDetail_FirstRecord_TypeChangeThrows()
    {
        var date = DateTime.Today.AddMonths(-2);
        var detail = new TrainingDetail
        {
            EmployeeId = "E001", LicenseType = "1.1", TrainingDate = date,
            TrainingType = 1, Hours = 0m
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetDetailAsync("E001", "1.1", date, default)).ReturnsAsync(detail);
        repoMock.Setup(r => r.GetDetailsAsync("E001", "1.1", default)).ReturnsAsync(new List<TrainingDetail> { detail });

        // 首筆改回訓 → 拒絕（首筆不變式）
        var req = new UpdateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(date), 2, 6m);
        await Assert.ThrowsAsync<InvalidOperationException>(() => BuildSvc(repoMock.Object).UpdateDetailAsync(req));
    }

    [Fact]
    public async Task UpdateDetail_FirstRecord_KeepType1_UpdatesHours()
    {
        var date = DateTime.Today.AddMonths(-2);
        var detail = new TrainingDetail
        {
            EmployeeId = "E001", LicenseType = "1.1", TrainingDate = date,
            TrainingType = 1, Hours = 0m
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetDetailAsync("E001", "1.1", date, default)).ReturnsAsync(detail);
        repoMock.Setup(r => r.GetDetailsAsync("E001", "1.1", default)).ReturnsAsync(new List<TrainingDetail> { detail });
        repoMock.Setup(r => r.UpdateDetailAsync(It.IsAny<TrainingDetail>(), default)).Returns(Task.CompletedTask);

        var req = new UpdateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(date), 1, 2m);
        var dto = await BuildSvc(repoMock.Object).UpdateDetailAsync(req);
        dto.Hours.Should().Be(2m);
    }

    // ── GetHeaders：排序 / 進階搜尋 ──────────────────────────────────────────

    private static TrainingHeader H(string employeeId, string licenseType, string? plant = null) =>
        new() { EmployeeId = employeeId, LicenseType = licenseType, Hours = 8, Plant = plant };

    private static Mock<ITrainingRepository> HeadersRepo(params TrainingHeader[] headers)
    {
        var repo = new Mock<ITrainingRepository>();
        repo.Setup(r => r.GetHeadersAsync(It.IsAny<string?>(), It.IsAny<string?>(), default))
            .ReturnsAsync(headers.ToList());
        repo.Setup(r => r.GetDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new List<TrainingDetail>());
        return repo;
    }

    [Fact]
    public async Task GetHeaders_DefaultSort_LicenseTypeThenEmployeeId()
    {
        // 未指定排序 → 預設證照升冪，同證照以員編為次要排序
        var repo = HeadersRepo(H("E002", "2.2"), H("E002", "1.1"), H("E001", "1.1"));

        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 1, 10);
        result.Items.Select(i => (i.LicenseType, i.EmployeeId))
            .Should().Equal(("1.1", "E001"), ("1.1", "E002"), ("2.2", "E002"));
    }

    [Fact]
    public async Task GetHeaders_DefaultSort_LicenseTypeIsNaturalOrder()
    {
        // 證照排序須為數值自然排序（與證照管理頁 NaturalSortKey 一致），非字串排序
        // 字串排序會錯排成 1.1, 11.2, 16.3, 20.1, 3.2
        var repo = HeadersRepo(
            H("E001", "20.1"), H("E001", "3.2"), H("E001", "11.2"), H("E001", "1.1"), H("E001", "16.3"));

        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 1, 10);
        result.Items.Select(i => i.LicenseType)
            .Should().Equal("1.1", "3.2", "11.2", "16.3", "20.1");
    }

    [Fact]
    public async Task GetHeaders_SortByLicenseType_Descending_NaturalOrder()
    {
        var repo = HeadersRepo(H("E001", "3.2"), H("E001", "11.2"), H("E001", "1.1"));
        var query = new TrainingSearchQuery { SortBy = "LicenseType", SortDesc = true };

        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 1, 10, query);
        result.Items.Select(i => i.LicenseType).Should().Equal("11.2", "3.2", "1.1");
    }

    [Fact]
    public async Task GetHeaders_SortByEmployeeId_SecondaryLicenseTypeIsNaturalOrder()
    {
        var repo = HeadersRepo(H("E001", "11.2"), H("E001", "3.2"), H("E001", "1.1"));
        var query = new TrainingSearchQuery { SortBy = "EmployeeId" };

        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 1, 10, query);
        result.Items.Select(i => i.LicenseType).Should().Equal("1.1", "3.2", "11.2");
    }

    [Fact]
    public async Task GetHeaders_SortByEmployeeId_Ascending_ThenByLicenseType()
    {
        var repo = HeadersRepo(H("E002", "1.1"), H("E001", "2.2"), H("E001", "1.1"));
        var query = new TrainingSearchQuery { SortBy = "EmployeeId" };

        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 1, 10, query);
        result.Items.Select(i => (i.EmployeeId, i.LicenseType))
            .Should().Equal(("E001", "1.1"), ("E001", "2.2"), ("E002", "1.1"));
    }

    [Fact]
    public async Task GetHeaders_SortByEmployeeId_Descending_SecondaryStaysAscending()
    {
        var repo = HeadersRepo(H("E001", "2.2"), H("E002", "1.1"), H("E001", "1.1"));
        var query = new TrainingSearchQuery { SortBy = "EmployeeId", SortDesc = true };

        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 1, 10, query);
        result.Items.Select(i => (i.EmployeeId, i.LicenseType))
            .Should().Equal(("E002", "1.1"), ("E001", "1.1"), ("E001", "2.2"));
    }

    [Fact]
    public async Task GetHeaders_SortByLicenseType_Ascending_ThenByEmployeeId()
    {
        var repo = HeadersRepo(H("E002", "1.1"), H("E001", "2.2"), H("E001", "1.1"));
        var query = new TrainingSearchQuery { SortBy = "LicenseType" };

        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 1, 10, query);
        result.Items.Select(i => (i.LicenseType, i.EmployeeId))
            .Should().Equal(("1.1", "E001"), ("1.1", "E002"), ("2.2", "E001"));
    }

    [Fact]
    public async Task GetHeaders_SortByLicenseType_Descending_SecondaryStaysAscending()
    {
        var repo = HeadersRepo(H("E002", "1.1"), H("E001", "2.2"), H("E001", "1.1"));
        var query = new TrainingSearchQuery { SortBy = "LicenseType", SortDesc = true };

        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 1, 10, query);
        result.Items.Select(i => (i.LicenseType, i.EmployeeId))
            .Should().Equal(("2.2", "E001"), ("1.1", "E001"), ("1.1", "E002"));
    }

    [Fact]
    public async Task GetHeaders_Sort_AppliedBeforePagination()
    {
        // 排序須套用於全清單而非當頁：3 筆、每頁 2 筆，第 2 頁應是排序後的最後一筆
        var repo = HeadersRepo(H("E003", "1.1"), H("E001", "1.1"), H("E002", "1.1"));
        var query = new TrainingSearchQuery { SortBy = "EmployeeId" };

        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 2, 2, query);
        result.Items.Select(i => i.EmployeeId).Should().Equal("E003");
    }

    [Fact]
    public async Task GetHeaders_SortBy_UnknownColumn_FallsBackToDefaultSort()
    {
        var repo = HeadersRepo(H("E002", "2.2"), H("E002", "1.1"), H("E001", "1.1"));
        var query = new TrainingSearchQuery { SortBy = "Remark" };

        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 1, 10, query);
        result.Items.Select(i => (i.LicenseType, i.EmployeeId))
            .Should().Equal(("1.1", "E001"), ("1.1", "E002"), ("2.2", "E002"));
    }

    [Fact]
    public void TrainingSearchQuery_SortByOnly_DoesNotActivateAdvanced()
    {
        new TrainingSearchQuery { SortBy = "EmployeeId", SortDesc = true }
            .IsAdvancedActive.Should().BeFalse();
    }

    [Fact]
    public void TrainingSearchQuery_Plant_ActivatesAdvanced()
    {
        new TrainingSearchQuery { Plant = "A1" }.IsAdvancedActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetHeaders_Advanced_PlantFilter_ReturnsMatchingOnly()
    {
        var repo = HeadersRepo(H("E001", "1.1", "A1"), H("E002", "1.1", "B2"), H("E003", "1.1", null));
        var query = new TrainingSearchQuery { Plant = "A1" };

        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 1, 10, query);
        result.Items.Select(i => i.EmployeeId).Should().Equal("E001");
    }

    [Fact]
    public async Task GetHeaders_Advanced_DepartmentFilter_IgnoresStoredWhitespace()
    {
        // DB 部門資料含尾端空白時仍應被「乾淨值」條件篩中（T4）
        var empRepo = new Mock<IEmployeeRepository>();
        empRepo.Setup(r => r.GetByIdAsync("E001", default))
            .ReturnsAsync(new Employee { EmployeeId = "E001", Name = "甲", Department = "製造部 " });
        empRepo.Setup(r => r.GetByIdAsync("E002", default))
            .ReturnsAsync(new Employee { EmployeeId = "E002", Name = "乙", Department = "品保部" });

        var repo = HeadersRepo(H("E001", "1.1"), H("E002", "1.1"));
        var query = new TrainingSearchQuery { Department = "製造部" };

        var result = await BuildSvc(repo.Object, empRepo: empRepo.Object).GetHeadersAsync(null, null, 1, 10, query);
        result.Items.Select(i => i.EmployeeId).Should().Equal("E001");
    }

    [Fact]
    public async Task GetHeaders_Advanced_DepartmentFilter_IgnoresQueryWhitespace()
    {
        // 查詢值含空白也應可比對（T4 防禦性 trim）
        var empRepo = new Mock<IEmployeeRepository>();
        empRepo.Setup(r => r.GetByIdAsync("E001", default))
            .ReturnsAsync(new Employee { EmployeeId = "E001", Name = "甲", Department = "製造部" });

        var repo = HeadersRepo(H("E001", "1.1"));
        var query = new TrainingSearchQuery { Department = " 製造部 " };

        var result = await BuildSvc(repo.Object, empRepo: empRepo.Object).GetHeadersAsync(null, null, 1, 10, query);
        result.Items.Select(i => i.EmployeeId).Should().Equal("E001");
    }
}
