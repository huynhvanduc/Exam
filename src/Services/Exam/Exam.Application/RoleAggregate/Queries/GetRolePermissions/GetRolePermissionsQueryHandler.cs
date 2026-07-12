using Exam.Domain.AggregateModels.RoleAggregate;
using MediatR;

namespace Exam.Application.RoleAggregate.Queries.GetRolePermissions;

public class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, IReadOnlyCollection<RolePermissionDto>>
{
    private readonly IRolePermissionRepository _rolePermissionRepository;

    public GetRolePermissionsQueryHandler(IRolePermissionRepository rolePermissionRepository)
    {
        _rolePermissionRepository = rolePermissionRepository;
    }

    public async Task<IReadOnlyCollection<RolePermissionDto>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var stored = await _rolePermissionRepository.GetAllAsync(cancellationToken);
        var storedByRole = stored.ToDictionary(x => x.Role);

        var result = new List<RolePermissionDto>();

        foreach (var role in new[] { UserRole.Student, UserRole.Instructor })
        {
            var permissions = storedByRole.TryGetValue(role, out var set)
                ? set.Permissions
                : [];
            result.Add(new RolePermissionDto(role, permissions));
        }

        // Admin luôn có toàn quyền (superuser bypass), không lưu trong DB.
        result.Add(new RolePermissionDto(UserRole.Admin, Permissions.All));

        return result;
    }
}
