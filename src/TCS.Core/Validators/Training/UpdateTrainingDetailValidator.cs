using FluentValidation;
using TCS.Core.DTOs.Requests;

namespace TCS.Core.Validators.Training;

/// <summary>修改受訓單身驗證（§9）</summary>
public class UpdateTrainingDetailValidator : AbstractValidator<UpdateTrainingDetailRequest>
{
    public UpdateTrainingDetailValidator()
    {
        RuleFor(x => x.TrainingType)
            .Must(t => t == 1 || t == 2)
                .WithMessage("受訓類型必須為 1（取得證照）或 2（回訓）");

        RuleFor(x => x.Hours)
            .GreaterThan(0m).WithMessage("受訓時數必須大於 0")
            .LessThanOrEqualTo(9999.9m).WithMessage("受訓時數不可超過 9999.9")
            .When(x => x.Hours.HasValue);
    }
}
