using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.PublishExam;

public class PublishExamCommandValidator : AbstractValidator<PublishExamCommand>
{
    public PublishExamCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
    }
}
