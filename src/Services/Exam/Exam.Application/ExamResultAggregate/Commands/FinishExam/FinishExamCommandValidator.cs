using FluentValidation;

namespace Exam.Application.ExamResultAggregate.Commands.FinishExam;

public class FinishExamCommandValidator : AbstractValidator<FinishExamCommand>
{
    public FinishExamCommandValidator()
    {
        RuleFor(x => x.ExamResultId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
