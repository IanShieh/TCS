using Moq;
using TCS.Core.DTOs.Requests;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Core.Services;
using Xunit;

namespace TCS.Tests.Services;

public class TrainingServiceTests
{
    [Fact]
    public async Task AddDetail_FirstRecord_MustBeType1()
    {
        var header = new TrainingHeader
        {
            EmployeeId = "E001", LicenseType = "1.1", RequiredHours = 16,
            Details = new List<TrainingDetail>()
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetHeaderAsync("E001", "1.1", true, default)).ReturnsAsync(header);

        var svc = new TrainingService(
            repoMock.Object,
            Mock.Of<ILicenseRepository>(),
            Mock.Of<IEmployeeRepository>(),
            new ExpiryCalculator());

        var req = new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)), 2, 8m);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddDetailAsync(req));
    }

    [Fact]
    public async Task CreateHeader_RequiredHours_FromLicense()
    {
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("1.1", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "1.1", Description = "Test", Hours = 24, Years = 2 });

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "1.1", default)).ReturnsAsync(false);
        trainingRepo.Setup(r => r.AddHeaderAsync(It.IsAny<TrainingHeader>(), default)).Returns(Task.CompletedTask);

        var svc = new TrainingService(
            trainingRepo.Object,
            licenseRepo.Object,
            Mock.Of<IEmployeeRepository>(),
            new ExpiryCalculator());

        var result = await svc.CreateHeaderAsync(new CreateTrainingHeaderRequest("E001", "1.1", null));
        Assert.Equal(24, result.RequiredHours);
    }

    [Fact]
    public async Task CreateHeader_AlreadyExists_ThrowsInvalidOperation()
    {
        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "1.1", default)).ReturnsAsync(true);

        var svc = new TrainingService(
            trainingRepo.Object,
            Mock.Of<ILicenseRepository>(),
            Mock.Of<IEmployeeRepository>(),
            new ExpiryCalculator());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateHeaderAsync(new CreateTrainingHeaderRequest("E001", "1.1", null)));
    }
}
