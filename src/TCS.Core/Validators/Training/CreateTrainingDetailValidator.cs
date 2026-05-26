using FluentValidation;
using TCS.Core.DTOs.Requests;

namespace TCS.Core.Validators.Training;

/// <summary>新增受訓單身驗證（§9）</summary>
public class CreateTrainingDetailValidator : AbstractValidator<CreateTrainingDetailRequest>
{
    public CreateTrainingDetailValidator()
    {
        RuleFor(x => x.TrainingDate)
            .NotEmpty().WithMessage("受訓日期為必填")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("受訓日期不可為未來日期");

        RuleFor(x => x.TrainingType)
            .Must(t => t == 1 || t == 2)
                .WithMessage("受訓類型必須為 1（取得證照）或 2（回訓）");

        RuleFor(x => x.Hours)
            .NotNull().WithMessage("回訓類型時，受訓時數為必填")
            .When(x => x.TrainingType == 2);

        RuleFor(x => x.Hours)
            .GreaterThan(0m).WithMessage("受訓時數必須大於 0")
            .LessThanOrEqualTo(9999.9m).WithMessage("受訓時數不可超過 9999.9")
            .When(x => x.Hours.HasValue);
    }
}
