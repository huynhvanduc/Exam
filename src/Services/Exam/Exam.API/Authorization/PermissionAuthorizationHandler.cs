using System.Security.Claims;
using Exam.Contracts;
using Exam.Domain.AggregateModels.RoleAggregate;
using Microsoft.AspNetCore.Authorization;

namespace Exam.API.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IRolePermissionRepository _rolePermissionRepository;

    public PermissionAuthorizationHandler(IRolePermissionRepository rolePermissionRepository)
    {
        _rolePermissionRepository = rolePermissionRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var roleClaim = context.User.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse<UserRole>(roleClaim, out var role))
            return;

        if (role == UserRole.Admin)
        {
            context.Succeed(requirement);
            return;
        }

        var rolePermissions = await _rolePermissionRepository.GetByRoleAsync(role);
        if (rolePermissions != null && rolePermissions.Has(requirement.Permission))
            context.Succeed(requirement);
    }
}
