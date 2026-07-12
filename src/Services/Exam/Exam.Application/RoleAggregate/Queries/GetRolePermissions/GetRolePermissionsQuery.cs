using MediatR;

namespace Exam.Application.RoleAggregate.Queries.GetRolePermissions;

public record GetRolePermissionsQuery : IRequest<IReadOnlyCollection<RolePermissionDto>>;
