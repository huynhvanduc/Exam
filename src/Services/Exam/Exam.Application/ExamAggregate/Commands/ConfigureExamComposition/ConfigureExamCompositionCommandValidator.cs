using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.ConfigureExamComposition;

public class ConfigureExamCompositionCommandValidator : AbstractValidator<ConfigureExamCompositionCommand>
{
    public ConfigureExamCompositionCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.Cells).NotNull();
        RuleForEach(x => x.Cells).Must(c => c.Count >= 0).WithMessage("Số câu mỗi ô không được âm.");
        RuleFor(x => x.Cells)
            .Must(cells => cells != null && cells.Sum(c => c.Count) > 0)
            .WithMessage("Tổng số câu trong ma trận phải lớn hơn 0.");
    }
}
