using Exam.Domain.Enums;
using FluentValidation;

namespace Exam.Application.QuestionAggregate.Commands.CreateQuestion;

public class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
{
    public CreateQuestionCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Explain).MaximumLength(2000);
        RuleFor(x => x.Points).GreaterThan(0);

        RuleFor(x => x.Answers).NotEmpty();
        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.Content).NotEmpty().MaximumLength(1000);
        });

        RuleFor(x => x)
            .Must(x => x.Answers.Any(a => a.IsCorrect))
            .WithMessage("At least one answer must be marked as correct.")
            .When(x => x.Answers != null && x.Answers.Count > 0);

        RuleFor(x => x)
            .Must(x => x.QuestionType != QuestionType.SingleSelection || x.Answers.Count(a => a.IsCorrect) <= 1)
            .WithMessage("A single selection question can only have one correct answer.")
            .When(x => x.Answers != null && x.Answers.Count > 0);
    }
}
