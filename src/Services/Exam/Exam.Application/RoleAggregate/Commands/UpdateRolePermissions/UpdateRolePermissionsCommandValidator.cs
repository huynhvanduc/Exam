using FluentValidation;

namespace Exam.Application.RoleAggregate.Commands.UpdateRolePermissions;

public class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleForEach(x => x.Permissions)
            .Must(p => Permissions.All.Contains(p))
            .WithMessage("'{PropertyValue}' is not a known permission key.");
    }
}
