using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.AddQuestionToExam;

public class AddQuestionToExamCommandValidator : AbstractValidator<AddQuestionToExamCommand>
{
    public AddQuestionToExamCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
    }
}
