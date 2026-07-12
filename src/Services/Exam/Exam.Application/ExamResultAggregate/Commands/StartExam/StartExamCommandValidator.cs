using FluentValidation;

namespace Exam.Application.ExamResultAggregate.Commands.StartExam;

public class StartExamCommandValidator : AbstractValidator<StartExamCommand>
{
    public StartExamCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
