using FluentAssertions;
using TCS.Core.DTOs.Requests;
using TCS.Core.Validators.License;
using TCS.Core.Validators.Training;

namespace TCS.Tests.Validators;

public class TrainingValidatorTests
{
    private readonly CreateTrainingHeaderValidator _header = new();
    private readonly CreateTrainingDetailValidator _createDetail = new();
    private readonly UpdateTrainingDetailValidator _updateDetail = new();
    private readonly CreateLicensePlantRequirementValidator _createPlantReq = new();
    private readonly UpdateLicensePlantRequirementValidator _updatePlantReq = new();

    // ── CreateTrainingHeaderValidator ──────────────────────────────────────

    [Fact]
    public void Header_Valid_IsValid()
    {
        _header.Validate(new CreateTrainingHeaderRequest("E001", "1.1", null, null)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Header_ValidWithRemark_IsValid()
    {
        _header.Validate(new CreateTrainingHeaderRequest("E001", "2.3", "備註說明", null)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Header_LargeCategoryLicenseType_IsInvalid()
    {
        var result = _header.Validate(new CreateTrainingHeaderRequest("E001", "1", null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LicenseType" && e.ErrorMessage.Contains("小類"));
    }

    [Fact]
    public void Header_EmptyEmployeeId_IsInvalid()
    {
        var result = _header.Validate(new CreateTrainingHeaderRequest("", "1.1", null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmployeeId");
    }

    [Fact]
    public void Header_EmployeeIdTooLong_IsInvalid()
    {
        var result = _header.Validate(new CreateTrainingHeaderRequest("12345678901", "1.1", null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmployeeId" && e.ErrorMessage.Contains("10 字元"));
    }

    [Fact]
    public void Header_RemarkTooLong_IsInvalid()
    {
        var result = _header.Validate(new CreateTrainingHeaderRequest("E001", "1.1", new string('A', 71), null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Remark" && e.ErrorMessage.Contains("70 字元"));
    }

    [Fact]
    public void Header_RemarkWithSqlInjection_IsInvalid()
    {
        var result = _header.Validate(new CreateTrainingHeaderRequest("E001", "1.1", "備註--DROP", null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Remark" && e.ErrorMessage.Contains("不允許的字元"));
    }

    [Fact]
    public void Header_LicenseTypeInvalidFormat_IsInvalid()
    {
        var result = _header.Validate(new CreateTrainingHeaderRequest("E001", "abc", null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LicenseType" && e.ErrorMessage.Contains("格式"));
    }

    [Fact]
    public void Header_Other_MajorCategoryWithCustomName_IsValid()
    {
        var req = new CreateTrainingHeaderRequest("E001", "99", "自定義證照", null, IsOther: true);
        _header.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Header_Other_MinorBaseCategory_IsValid()
    {
        var req = new CreateTrainingHeaderRequest("E001", "1", "自定義證照", null, IsOther: true);
        _header.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Header_Other_MissingCustomName_IsInvalid()
    {
        var req = new CreateTrainingHeaderRequest("E001", "99", null, null, IsOther: true);
        var result = _header.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Remark");
    }

    [Fact]
    public void Header_NonOther_IntegerCategory_StillInvalid()
    {
        var req = new CreateTrainingHeaderRequest("E001", "1", null, null);
        _header.Validate(req).IsValid.Should().BeFalse();
    }

    // ── CreateTrainingDetailValidator ──────────────────────────────────────

    [Fact]
    public void Detail_ValidType1Today_IsValid()
    {
        var req = new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today), 1, 8m);
        _createDetail.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Detail_ValidType2Past_IsValid()
    {
        var req = new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today.AddDays(-30)), 2, 4m);
        _createDetail.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Detail_FutureDateIsInvalid()
    {
        var req = new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today.AddDays(1)), 1, 8m);
        var result = _createDetail.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TrainingDate" && e.ErrorMessage.Contains("未來日期"));
    }

    [Fact]
    public void Detail_TrainingTypeZero_IsInvalid()
    {
        var result = _createDetail.Validate(new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today), 0, 8m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TrainingType");
    }

    [Fact]
    public void Detail_TrainingTypeThree_IsInvalid()
    {
        var result = _createDetail.Validate(new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today), 3, 8m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TrainingType");
    }

    [Fact]
    public void Detail_ZeroHours_IsInvalid()
    {
        var result = _createDetail.Validate(new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today), 1, 0m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Hours");
    }

    [Fact]
    public void Detail_NegativeHours_IsInvalid()
    {
        var result = _createDetail.Validate(new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today), 1, -1m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Hours");
    }

    [Fact]
    public void Detail_ExceedMaxHours_IsInvalid()
    {
        var result = _createDetail.Validate(new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today), 1, 10000m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Hours" && e.ErrorMessage.Contains("9999.9"));
    }

    // ── UpdateTrainingDetailValidator ──────────────────────────────────────

    [Fact]
    public void UpdateDetail_Valid_IsValid()
    {
        var req = new UpdateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today), 1, 8m);
        _updateDetail.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetail_InvalidTrainingType_IsInvalid()
    {
        var result = _updateDetail.Validate(new UpdateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today), 0, 8m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TrainingType");
    }

    [Fact]
    public void UpdateDetail_ZeroHours_IsInvalid()
    {
        var result = _updateDetail.Validate(new UpdateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today), 1, 0m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Hours");
    }

    [Fact]
    public void UpdateDetail_ExceedMaxHours_IsInvalid()
    {
        var result = _updateDetail.Validate(new UpdateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(DateTime.Today), 2, 9999.91m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Hours" && e.ErrorMessage.Contains("9999.9"));
    }

    // ── CreateLicensePlantRequirementValidator ─────────────────────────────

    [Fact]
    public void PlantReq_Valid_IsValid()
    {
        _createPlantReq.Validate(new CreateLicensePlantRequirementRequest("1.1", "P001", 5)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void PlantReq_LargeCategoryLicenseType_IsInvalid()
    {
        var result = _createPlantReq.Validate(new CreateLicensePlantRequirementRequest("1", "P001", 5));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LicenseType" && e.ErrorMessage.Contains("小類"));
    }

    [Fact]
    public void PlantReq_EmptyPlant_IsInvalid()
    {
        var result = _createPlantReq.Validate(new CreateLicensePlantRequirementRequest("1.1", "", 5));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Plant");
    }

    [Fact]
    public void PlantReq_PlantTooLong_IsInvalid()
    {
        var result = _createPlantReq.Validate(new CreateLicensePlantRequirementRequest("1.1", "TOOLONG", 5));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Plant" && e.ErrorMessage.Contains("6 字元"));
    }

    [Fact]
    public void PlantReq_NegativeRequiredCount_IsInvalid()
    {
        var result = _createPlantReq.Validate(new CreateLicensePlantRequirementRequest("1.1", "P001", -1));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RequiredCount");
    }

    [Fact]
    public void PlantReq_ZeroRequiredCount_IsValid()
    {
        _createPlantReq.Validate(new CreateLicensePlantRequirementRequest("1.1", "P001", 0)).IsValid.Should().BeTrue();
    }

    // ── UpdateLicensePlantRequirementValidator ─────────────────────────────

    [Fact]
    public void UpdatePlantReq_ZeroCount_IsValid()
    {
        _updatePlantReq.Validate(new UpdateLicensePlantRequirementRequest("1.1", "P001", 0)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdatePlantReq_PositiveCount_IsValid()
    {
        _updatePlantReq.Validate(new UpdateLicensePlantRequirementRequest("1.1", "P001", 10)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdatePlantReq_NegativeCount_IsInvalid()
    {
        var result = _updatePlantReq.Validate(new UpdateLicensePlantRequirementRequest("1.1", "P001", -1));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RequiredCount");
    }
}
