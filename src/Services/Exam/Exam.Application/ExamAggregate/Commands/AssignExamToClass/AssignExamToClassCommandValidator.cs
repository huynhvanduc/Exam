using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.AssignExamToClass;

public class AssignExamToClassCommandValidator : AbstractValidator<AssignExamToClassCommand>
{
    public AssignExamToClassCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.ClassId).NotEmpty();
    }
}
