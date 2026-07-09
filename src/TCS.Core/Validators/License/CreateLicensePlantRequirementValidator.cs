using FluentValidation;
using TCS.Core.DTOs.Requests;

namespace TCS.Core.Validators.License;

/// <summary>新增證照廠別需求驗證（§9）</summary>
public class CreateLicensePlantRequirementValidator : AbstractValidator<CreateLicensePlantRequirementRequest>
{
    public CreateLicensePlantRequirementValidator()
    {
        RuleFor(x => x.LicenseType)
            .NotEmpty().WithMessage("證照類別代碼為必填")
            .MaximumLength(10).WithMessage("證照類別代碼不可超過 10 字元")
            .Must(ValidatorHelpers.IsValidLicenseTypeFormat)
                .WithMessage("證照類別代碼格式不正確")
            // 廠別需求只能對應小類（§9）
            .Must(lt => !ValidatorHelpers.IsLicenseTypeCategory(lt))
                .WithMessage("廠別需求的證照類別必須為小類（含小數點）");

        RuleFor(x => x.Plant)
            .NotEmpty().WithMessage("廠別代碼為必填")
            .MaximumLength(6).WithMessage("廠別代碼不可超過 6 字元")
            .Must(ValidatorHelpers.IsSafe).WithMessage("廠別代碼含有不允許的字元");

        RuleFor(x => x.RequiredCount)
            .GreaterThanOrEqualTo(1).WithMessage("需求數量必須為 1 以上");
    }
}
