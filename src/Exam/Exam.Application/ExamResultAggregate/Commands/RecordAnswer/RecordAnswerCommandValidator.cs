using FluentValidation;

namespace Exam.Application.ExamResultAggregate.Commands.RecordAnswer;

public class RecordAnswerCommandValidator : AbstractValidator<RecordAnswerCommand>
{
    public RecordAnswerCommandValidator()
    {
        RuleFor(x => x.ExamResultId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.SelectedAnswerIds).NotNull();
    }
}
