using FluentValidation;
using TCS.Core.DTOs.Requests;

namespace TCS.Core.Validators.License;

/// <summary>修改證照廠別需求驗證（§9）</summary>
public class UpdateLicensePlantRequirementValidator : AbstractValidator<UpdateLicensePlantRequirementRequest>
{
    public UpdateLicensePlantRequirementValidator()
    {
        RuleFor(x => x.RequiredCount)
            .GreaterThanOrEqualTo(1).WithMessage("需求數量必須為 1 以上");
    }
}
