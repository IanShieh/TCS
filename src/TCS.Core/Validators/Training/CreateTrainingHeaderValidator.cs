using FluentValidation;
using TCS.Core.DTOs.Requests;

namespace TCS.Core.Validators.Training;

/// <summary>新增受訓單頭驗證（§9）</summary>
public class CreateTrainingHeaderValidator : AbstractValidator<CreateTrainingHeaderRequest>
{
    public CreateTrainingHeaderValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("員工編號為必填")
            .MaximumLength(10).WithMessage("員工編號不可超過 10 字元")
            .Must(ValidatorHelpers.IsSafe).WithMessage("員工編號含有不允許的字元");

        RuleFor(x => x.LicenseType)
            .NotEmpty().WithMessage("證照類別代碼為必填")
            .MaximumLength(10).WithMessage("證照類別代碼不可超過 10 字元")
            .Must(ValidatorHelpers.IsValidLicenseTypeFormat)
                .WithMessage("證照類別代碼格式不正確")
            // 單頭只能對應小類（§9）
            .Must(lt => !ValidatorHelpers.IsLicenseTypeCategory(lt))
                .WithMessage("受訓單頭的證照類別必須為小類（含小數點）");

        RuleFor(x => x.Remark)
            .MaximumLength(70).WithMessage("備註不可超過 70 字元")
            .Must(ValidatorHelpers.IsSafe).WithMessage("備註含有不允許的字元")
            .When(x => x.Remark is not null);
    }
}
