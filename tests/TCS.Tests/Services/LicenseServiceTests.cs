using FluentAssertions;
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
        mock.Setup(r => r.GetByIdAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string lt, CancellationToken _) => data.FirstOrDefault(l => l.LicenseType == lt));
        return mock.Object;
    }

    // ── GetAll ─────────────────────────────────────────────────────────────

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
    public async Task GetAll_NoSearch_ReturnsAll()
    {
        var data = new List<LicenseMaster>
        {
            new() { LicenseType = "1", Description = "電氣類" },
            new() { LicenseType = "1.1", Description = "低壓電氣作業", Category = "1", Hours = 8, Years = 2 },
            new() { LicenseType = "2", Description = "機械類" }
        };
        var svc = new LicenseService(CreateMockRepo(data), Mock.Of<IPlantRepository>());
        var result = await svc.GetAllAsync(1, 10);
        result.Items.Should().HaveCount(3);
        result.TotalItems.Should().Be(3);
    }

    // ── GetById ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Existing_ReturnsMappedDto()
    {
        var data = new List<LicenseMaster>
        {
            new() { LicenseType = "1.1", Description = "低壓電氣作業", Category = "1", Hours = 16, Years = 2 }
        };
        var svc = new LicenseService(CreateMockRepo(data), Mock.Of<IPlantRepository>());
        var dto = await svc.GetByIdAsync("1.1");
        dto.LicenseType.Should().Be("1.1");
        dto.Hours.Should().Be(16);
        dto.Years.Should().Be(2);
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsKeyNotFound()
    {
        var svc = new LicenseService(CreateMockRepo([]), Mock.Of<IPlantRepository>());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync("9.9"));
    }

    // ── Create ─────────────────────────────────────────────────────────────

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

    [Fact]
    public async Task Create_NewLicenseType_ReturnsMappedDto()
    {
        var mock = new Mock<ILicenseRepository>();
        mock.Setup(r => r.ExistsAsync("1.2", default)).ReturnsAsync(false);
        mock.Setup(r => r.AddAsync(It.IsAny<LicenseMaster>(), default)).Returns(Task.CompletedTask);
        var svc = new LicenseService(mock.Object, Mock.Of<IPlantRepository>());
        var dto = await svc.CreateAsync(new CreateLicenseMasterRequest("1.2", "高壓電氣作業", "1", 24, 3));
        dto.LicenseType.Should().Be("1.2");
        dto.Description.Should().Be("高壓電氣作業");
        dto.Hours.Should().Be(24);
    }

    // ── Update ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_NotFound_ThrowsKeyNotFound()
    {
        var mock = new Mock<ILicenseRepository>();
        mock.Setup(r => r.GetByIdAsync("9.9", default)).ReturnsAsync((LicenseMaster?)null);
        var svc = new LicenseService(mock.Object, Mock.Of<IPlantRepository>());
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.UpdateAsync(new UpdateLicenseMasterRequest("9.9", "描述", "9", 8, 2)));
    }

    [Fact]
    public async Task Update_Existing_UpdatesFieldsAndReturnsDto()
    {
        var entity = new LicenseMaster { LicenseType = "1.1", Description = "舊描述", Category = "1", Hours = 8, Years = 2 };
        var mock = new Mock<ILicenseRepository>();
        mock.Setup(r => r.GetByIdAsync("1.1", default)).ReturnsAsync(entity);
        mock.Setup(r => r.UpdateAsync(It.IsAny<LicenseMaster>(), default)).Returns(Task.CompletedTask);
        var svc = new LicenseService(mock.Object, Mock.Of<IPlantRepository>());
        var dto = await svc.UpdateAsync(new UpdateLicenseMasterRequest("1.1", "新描述", "1", 16, 3));
        dto.Description.Should().Be("新描述");
        dto.Hours.Should().Be(16);
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithTrainingHeaders_ThrowsInvalidOperation()
    {
        var mock = new Mock<ILicenseRepository>();
        mock.Setup(r => r.HasTrainingHeadersAsync("1.1", default)).ReturnsAsync(true);
        var svc = new LicenseService(mock.Object, Mock.Of<IPlantRepository>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteAsync("1.1"));
    }

    [Fact]
    public async Task Delete_NoTrainingHeaders_CallsRepo()
    {
        var mock = new Mock<ILicenseRepository>();
        mock.Setup(r => r.HasTrainingHeadersAsync("1.1", default)).ReturnsAsync(false);
        mock.Setup(r => r.DeleteAsync("1.1", default)).Returns(Task.CompletedTask);
        var svc = new LicenseService(mock.Object, Mock.Of<IPlantRepository>());
        await svc.DeleteAsync("1.1");
        mock.Verify(r => r.DeleteAsync("1.1", default), Times.Once);
    }

    // ── PlantRequirements ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPlantRequirements_MapsPlantName()
    {
        var requirements = new List<LicensePlantRequirement>
        {
            new() { LicenseType = "1.1", Plant = "P01", RequiredCount = 5 }
        };
        var plants = new List<Plant>
        {
            new() { PlantCode = "P01", PlantName = "台中廠" }
        };
        var licRepo = new Mock<ILicenseRepository>();
        licRepo.Setup(r => r.GetPlantRequirementsAsync("1.1", default)).ReturnsAsync(requirements);
        var plantRepo = new Mock<IPlantRepository>();
        plantRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(plants);
        var svc = new LicenseService(licRepo.Object, plantRepo.Object);
        var dtos = await svc.GetPlantRequirementsAsync("1.1");
        dtos.Should().HaveCount(1);
        dtos[0].Plant.Should().Be("P01");
        dtos[0].PlantName.Should().Be("台中廠");
        dtos[0].RequiredCount.Should().Be(5);
    }

    [Fact]
    public async Task CreatePlantRequirement_Duplicate_ThrowsInvalidOperation()
    {
        var existing = new LicensePlantRequirement { LicenseType = "1.1", Plant = "P01", RequiredCount = 3 };
        var licRepo = new Mock<ILicenseRepository>();
        licRepo.Setup(r => r.GetPlantRequirementAsync("1.1", "P01", default)).ReturnsAsync(existing);
        var svc = new LicenseService(licRepo.Object, Mock.Of<IPlantRepository>());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreatePlantRequirementAsync(new CreateLicensePlantRequirementRequest("1.1", "P01", 5)));
    }

    [Fact]
    public async Task UpdatePlantRequirement_NotFound_ThrowsKeyNotFound()
    {
        var licRepo = new Mock<ILicenseRepository>();
        licRepo.Setup(r => r.GetPlantRequirementAsync("1.1", "P99", default)).ReturnsAsync((LicensePlantRequirement?)null);
        var svc = new LicenseService(licRepo.Object, Mock.Of<IPlantRepository>());
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.UpdatePlantRequirementAsync(new UpdateLicensePlantRequirementRequest("1.1", "P99", 10)));
    }
}
