using FluentValidation;

namespace Exam.Application.UserAggregate.Commands.ToggleUserActive;

public class ToggleUserActiveCommandValidator : AbstractValidator<ToggleUserActiveCommand>
{
    public ToggleUserActiveCommandValidator()
    {
        RuleFor(x => x.ExternalId).NotEmpty();
    }
}
