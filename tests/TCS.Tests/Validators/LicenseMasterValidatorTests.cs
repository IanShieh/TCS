using FluentAssertions;
using TCS.Core.DTOs.Requests;
using TCS.Core.Validators.License;

namespace TCS.Tests.Validators;

public class LicenseMasterValidatorTests
{
    private readonly CreateLicenseMasterValidator _create = new();
    private readonly UpdateLicenseMasterValidator _update = new();

    // ── CreateLicenseMasterValidator ───────────────────────────────────────

    [Fact]
    public void Create_ValidLargeCategory_IsValid()
    {
        var req = new CreateLicenseMasterRequest("1", "電氣類", null, null, null);
        _create.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_ValidSmallCategory_IsValid()
    {
        var req = new CreateLicenseMasterRequest("1.1", "低壓電氣作業", "1", 8, 2);
        _create.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyLicenseType_IsInvalid()
    {
        var result = _create.Validate(new CreateLicenseMasterRequest("", "電氣類", null, null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LicenseType");
    }

    [Fact]
    public void Create_LicenseTypeTooLong_IsInvalid()
    {
        var result = _create.Validate(new CreateLicenseMasterRequest("12345678901", "電氣類", null, null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LicenseType" && e.ErrorMessage.Contains("10 字元"));
    }

    [Fact]
    public void Create_LicenseTypeInvalidFormat_IsInvalid()
    {
        var result = _create.Validate(new CreateLicenseMasterRequest("abc", "電氣類", null, null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LicenseType" && e.ErrorMessage.Contains("格式"));
    }

    [Fact]
    public void Create_LargeCategoryWithCategorySet_IsInvalid()
    {
        var result = _create.Validate(new CreateLicenseMasterRequest("1", "電氣類", "2", null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category" && e.ErrorMessage.Contains("大類列"));
    }

    [Fact]
    public void Create_SmallCategoryWithoutCategory_IsInvalid()
    {
        var result = _create.Validate(new CreateLicenseMasterRequest("1.1", "低壓電氣作業", null, 8, 2));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category" && e.ErrorMessage.Contains("小類列"));
    }

    [Fact]
    public void Create_SmallCategoryWithNonIntegerCategory_IsInvalid()
    {
        // Category must be pure integer (大類代碼)
        var result = _create.Validate(new CreateLicenseMasterRequest("1.1", "低壓電氣作業", "1.1", 8, 2));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category" && e.ErrorMessage.Contains("純整數格式"));
    }

    [Fact]
    public void Create_SmallCategoryWithNullHours_IsValid()
    {
        // Hours 不再必填
        var result = _create.Validate(new CreateLicenseMasterRequest("1.1", "低壓電氣作業", "1", null, 2));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_SmallCategoryWithZeroHours_IsInvalid()
    {
        var result = _create.Validate(new CreateLicenseMasterRequest("1.1", "低壓電氣作業", "1", 0, 2));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Hours");
    }

    [Fact]
    public void Create_SmallCategoryWithNullYears_IsValid()
    {
        // Years 不再必填
        var result = _create.Validate(new CreateLicenseMasterRequest("1.1", "低壓電氣作業", "1", 8, null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_SmallCategoryWithZeroYears_IsInvalid()
    {
        var result = _create.Validate(new CreateLicenseMasterRequest("1.1", "低壓電氣作業", "1", 8, 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Years");
    }

    [Fact]
    public void Create_EmptyDescription_IsInvalid()
    {
        var result = _create.Validate(new CreateLicenseMasterRequest("1", "", null, null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Create_DescriptionTooLong_IsInvalid()
    {
        var result = _create.Validate(new CreateLicenseMasterRequest("1", new string('A', 71), null, null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage.Contains("70 字元"));
    }

    [Fact]
    public void Create_DescriptionWithSqlInjection_IsInvalid()
    {
        var result = _create.Validate(new CreateLicenseMasterRequest("1", "Test--Drop", null, null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage.Contains("不允許的字元"));
    }

    [Fact]
    public void Create_LargeCategoryWithOptionalHoursAndYears_IsValid()
    {
        // 大類可選填 Hours/Years
        var req = new CreateLicenseMasterRequest("2", "機械類", null, 16, 3);
        _create.Validate(req).IsValid.Should().BeTrue();
    }

    // ── UpdateLicenseMasterValidator ───────────────────────────────────────

    [Fact]
    public void Update_ValidSmallCategory_IsValid()
    {
        var req = new UpdateLicenseMasterRequest("1.1", "低壓電氣作業（更新）", "1", 16, 2);
        _update.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Update_ValidLargeCategory_IsValid()
    {
        var req = new UpdateLicenseMasterRequest("1", "電氣類（更新）", null, null, null);
        _update.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Update_EmptyDescription_IsInvalid()
    {
        var result = _update.Validate(new UpdateLicenseMasterRequest("1.1", "", "1", 16, 2));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Update_SmallCategoryWithoutCategory_IsInvalid()
    {
        var result = _update.Validate(new UpdateLicenseMasterRequest("1.1", "描述", null, 8, 2));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public void Update_LargeCategoryWithCategorySet_IsInvalid()
    {
        var result = _update.Validate(new UpdateLicenseMasterRequest("1", "電氣類", "1", null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category" && e.ErrorMessage.Contains("大類列"));
    }

    [Fact]
    public void Update_SmallCategoryMissingHours_IsValid()
    {
        // Hours 不再必填
        var result = _update.Validate(new UpdateLicenseMasterRequest("1.1", "描述", "1", null, 2));
        result.IsValid.Should().BeTrue();
    }
}
