using MediatR;

namespace Exam.Application.RoleAggregate.Commands.UpdateRolePermissions;

public record UpdateRolePermissionsCommand(UserRole Role, IReadOnlyCollection<string> Permissions, Actor Actor) : IRequest<RolePermissionDto>;
