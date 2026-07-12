using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.DeleteExam;

public class DeleteExamCommandValidator : AbstractValidator<DeleteExamCommand>
{
    public DeleteExamCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
