using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.UnassignExamFromClass;

public class UnassignExamFromClassCommandValidator : AbstractValidator<UnassignExamFromClassCommand>
{
    public UnassignExamFromClassCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.ClassId).NotEmpty();
    }
}
