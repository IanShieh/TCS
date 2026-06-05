using FluentValidation;
using TCS.Core.DTOs.Requests;

namespace TCS.Core.Validators.License;

/// <summary>修改證照主檔驗證（§9）— LicenseType 由 route 帶入後填入 request</summary>
public class UpdateLicenseMasterValidator : AbstractValidator<UpdateLicenseMasterRequest>
{
    public UpdateLicenseMasterValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("描述為必填")
            .MaximumLength(70).WithMessage("描述不可超過 70 字元")
            .Must(ValidatorHelpers.IsSafe).WithMessage("描述含有不允許的字元");

        When(x => ValidatorHelpers.IsLicenseTypeCategory(x.LicenseType), () =>
        {
            RuleFor(x => x.Category)
                .Null().WithMessage("大類列的「對應大類」必須為空");
        });

        When(x => !ValidatorHelpers.IsLicenseTypeCategory(x.LicenseType) &&
                   ValidatorHelpers.IsValidLicenseTypeFormat(x.LicenseType), () =>
        {
            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("小類列的「對應大類」為必填")
                .MaximumLength(10).WithMessage("對應大類代碼不可超過 10 字元")
                .Must(ValidatorHelpers.IsLicenseTypeCategory!)
                    .WithMessage("對應大類代碼必須為純整數格式")
                .Must(ValidatorHelpers.IsSafe).WithMessage("對應大類含有不允許的字元");
        });

        // Hours/Years 選填；填寫時須符合範圍
        When(x => x.Hours.HasValue, () =>
        {
            RuleFor(x => x.Hours)
                .GreaterThan(0).WithMessage("時數必須大於 0");
        });

        When(x => x.Years.HasValue, () =>
        {
            RuleFor(x => x.Years)
                .GreaterThanOrEqualTo(1).WithMessage("年數必須 ≥ 1");
        });
    }
}
