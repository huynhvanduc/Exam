using FluentValidation;

namespace Exam.Application.ClassAggregate.Commands.JoinClass;

public class JoinClassCommandValidator : AbstractValidator<JoinClassCommand>
{
    public JoinClassCommandValidator()
    {
        RuleFor(x => x.JoinCode).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
