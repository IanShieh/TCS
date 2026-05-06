using DingxinErp.Core.DTOs;
using FluentValidation;

namespace DingxinErp.Core.Validators;

/// <summary>
/// 新增單頭驗證器
/// </summary>
public class CreateSampleHeaderValidator : AbstractValidator<CreateSampleHeaderRequest>
{
    public CreateSampleHeaderValidator()
    {
        RuleFor(x => x.TA001)
            .NotEmpty().WithMessage("單別為必填")
            .MaximumLength(4).WithMessage("單別最多 4 碼");

        RuleFor(x => x.TA002)
            .NotEmpty().WithMessage("單號為必填")
            .MaximumLength(11).WithMessage("單號最多 11 碼");

        RuleFor(x => x.TA003)
            .NotEmpty().WithMessage("日期為必填")
            .Length(8).WithMessage("日期格式為 yyyyMMdd (8碼)")
            .Matches(@"^\d{8}$").WithMessage("日期必須為 8 位數字");

        RuleFor(x => x.TA004)
            .NotEmpty().WithMessage("客戶/供應商代號為必填")
            .MaximumLength(10).WithMessage("客戶/供應商代號最多 10 碼");

        RuleFor(x => x.TA007)
            .Must(v => v == "Y" || v == "N").WithMessage("狀態碼必須為 Y 或 N");
    }
}

public class CreateSampleDetailValidator : AbstractValidator<CreateSampleDetailRequest>
{
    public CreateSampleDetailValidator()
    {
        RuleFor(x => x.TB003)
            .NotEmpty().WithMessage("序號為必填")
            .MaximumLength(4).WithMessage("序號最多 4 碼");

        RuleFor(x => x.TB004)
            .NotEmpty().WithMessage("品號為必填")
            .MaximumLength(20).WithMessage("品號最多 20 碼");

        RuleFor(x => x.TB006)
            .GreaterThan(0).WithMessage("數量必須大於 0");

        RuleFor(x => x.TB007)
            .GreaterThanOrEqualTo(0).WithMessage("單價不可為負數");
    }
}
