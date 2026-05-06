using FluentValidation;
using TCS.Core.DTOs.Requests;

namespace TCS.Core.Validators.License;

/// <summary>修改證照廠別需求驗證（§9）</summary>
public class UpdateLicensePlantRequirementValidator : AbstractValidator<UpdateLicensePlantRequirementRequest>
{
    public UpdateLicensePlantRequirementValidator()
    {
        RuleFor(x => x.RequiredCount)
            .GreaterThanOrEqualTo(0).WithMessage("需求數量不可小於 0");
    }
}
