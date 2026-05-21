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
    public async Task AddDetail_HeaderNotFound_ThrowsKeyNotFound()
    {
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetHeaderAsync("E001", "9.9", true, default)).ReturnsAsync((TrainingHeader?)null);

        var req = new CreateTrainingDetailRequest("E001", "9.9", DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)), 1, 8m);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => BuildSvc(repoMock.Object).AddDetailAsync(req));
    }
}
