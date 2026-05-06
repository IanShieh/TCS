using FluentValidation;
using TCS.Core.DTOs.Requests;

namespace TCS.Core.Validators.Training;

/// <summary>修改受訓單頭驗證（§9）— 僅允許改 Remark</summary>
public class UpdateTrainingHeaderValidator : AbstractValidator<UpdateTrainingHeaderRequest>
{
    public UpdateTrainingHeaderValidator()
    {
        RuleFor(x => x.Remark)
            .MaximumLength(70).WithMessage("備註不可超過 70 字元")
            .Must(ValidatorHelpers.IsSafe).WithMessage("備註含有不允許的字元")
            .When(x => x.Remark is not null);
    }
}
