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
    public async Task AddDetail_SecondRecordType1_ThrowsInvalidOperation()
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

        var req = new CreateTrainingDetailRequest(
            "E001", "1.1", DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)), 1, 4m);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => BuildSvc(repoMock.Object).AddDetailAsync(req));
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
    public async Task UpdateDetail_DoesNotChangeTrainingType_OnlyHours()
    {
        var date = DateTime.Today.AddMonths(-2);
        var detail = new TrainingDetail
        {
            EmployeeId = "E001", LicenseType = "1.1", TrainingDate = date,
            TrainingType = 2, Hours = 4m
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetDetailAsync("E001", "1.1", date, default)).ReturnsAsync(detail);
        repoMock.Setup(r => r.UpdateDetailAsync(It.IsAny<TrainingDetail>(), default)).Returns(Task.CompletedTask);

        // 請求嘗試把 type 改成 1，且改時數為 6
        var req = new UpdateTrainingDetailRequest(
            "E001", "1.1", DateOnly.FromDateTime(date), 1, 6m);
        var dto = await BuildSvc(repoMock.Object).UpdateDetailAsync(req);

        dto.TrainingType.Should().Be(2);   // type 鎖定，忽略請求的 1
        dto.Hours.Should().Be(6m);         // hours 仍更新
    }
}
