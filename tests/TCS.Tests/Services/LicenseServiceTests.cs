using Moq;
using TCS.Core.DTOs.Requests;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Core.Services;
using Xunit;

namespace TCS.Tests.Services;

public class LicenseServiceTests
{
    private static ILicenseRepository CreateMockRepo(List<LicenseMaster> data)
    {
        var mock = new Mock<ILicenseRepository>();
        mock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(data);
        mock.Setup(r => r.ExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string lt, CancellationToken _) => data.Any(l => l.LicenseType == lt));
        mock.Setup(r => r.HasTrainingHeadersAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);
        return mock.Object;
    }

    [Fact]
    public async Task GetAll_SearchFilters_CorrectResults()
    {
        var data = new List<LicenseMaster>
        {
            new() { LicenseType = "1", Description = "電氣類", Category = null },
            new() { LicenseType = "1.1", Description = "低壓電氣作業", Category = "1", Hours = 16, Years = 2 }
        };
        var svc = new LicenseService(CreateMockRepo(data), Mock.Of<IPlantRepository>());
        var result = await svc.GetAllAsync(1, 10, "低壓");
        Assert.Single(result.Items);
        Assert.Equal("1.1", result.Items[0].LicenseType);
    }

    [Fact]
    public async Task Delete_WithTrainingHeaders_ThrowsInvalidOperation()
    {
        var mock = new Mock<ILicenseRepository>();
        mock.Setup(r => r.HasTrainingHeadersAsync("1.1", default)).ReturnsAsync(true);
        var svc = new LicenseService(mock.Object, Mock.Of<IPlantRepository>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteAsync("1.1"));
    }

    [Fact]
    public async Task Create_DuplicateLicenseType_ThrowsInvalidOperation()
    {
        var data = new List<LicenseMaster>
        {
            new() { LicenseType = "1.1", Description = "低壓電氣作業", Category = "1" }
        };
        var svc = new LicenseService(CreateMockRepo(data), Mock.Of<IPlantRepository>());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateLicenseMasterRequest("1.1", "Test", "1", 8, 1)));
    }
}
