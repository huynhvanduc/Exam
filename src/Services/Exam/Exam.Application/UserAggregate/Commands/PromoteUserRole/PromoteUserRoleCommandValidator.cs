using FluentValidation;

namespace Exam.Application.UserAggregate.Commands.PromoteUserRole;

public class PromoteUserRoleCommandValidator : AbstractValidator<PromoteUserRoleCommand>
{
    public PromoteUserRoleCommandValidator()
    {
        RuleFor(x => x.ExternalId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}
