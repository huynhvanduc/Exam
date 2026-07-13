using FluentValidation;

namespace Exam.Application.QuestionAggregate.Commands.ImportQuestions;

public class ImportQuestionsCommandValidator : AbstractValidator<ImportQuestionsCommand>
{
    public ImportQuestionsCommandValidator()
    {
        RuleFor(x => x.FileContent).NotEmpty().WithMessage("File is required.");
        RuleFor(x => x.OwnerUserId).NotEmpty();
    }
}
