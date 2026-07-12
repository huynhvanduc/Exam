using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.UnpublishExam;

public class UnpublishExamCommandValidator : AbstractValidator<UnpublishExamCommand>
{
    public UnpublishExamCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
    }
}
