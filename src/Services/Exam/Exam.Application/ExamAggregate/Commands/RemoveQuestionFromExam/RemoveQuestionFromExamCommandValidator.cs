using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.RemoveQuestionFromExam;

public class RemoveQuestionFromExamCommandValidator : AbstractValidator<RemoveQuestionFromExamCommand>
{
    public RemoveQuestionFromExamCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
    }
}
