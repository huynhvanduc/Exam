using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.ArchiveExam;

public class ArchiveExamCommandValidator : AbstractValidator<ArchiveExamCommand>
{
    public ArchiveExamCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
    }
}
