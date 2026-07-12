using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.CreateExam;

public class CreateExamCommandValidator : AbstractValidator<CreateExamCommand>
{
    public CreateExamCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortDesc).MaximumLength(500);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Duration).GreaterThan(TimeSpan.Zero);
        RuleFor(x => x.MinimumPassingScore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OwnerUserId).NotEmpty();
    }
}
