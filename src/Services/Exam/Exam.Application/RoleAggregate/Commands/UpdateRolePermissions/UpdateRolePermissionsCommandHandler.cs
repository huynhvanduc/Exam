using Exam.Domain.AggregateModels.RoleAggregate;
using Exam.Domain.Exceptions;
using MediatR;

namespace Exam.Application.RoleAggregate.Commands.UpdateRolePermissions;

public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, RolePermissionDto>
{
    private readonly IRolePermissionRepository _rolePermissionRepository;

    public UpdateRolePermissionsCommandHandler(IRolePermissionRepository rolePermissionRepository)
    {
        _rolePermissionRepository = rolePermissionRepository;
    }

    public async Task<RolePermissionDto> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        if (request.Role == UserRole.Admin)
            throw new ExamDomainException("Admin always has full access; its permission set cannot be changed.");

        var existing = await _rolePermissionRepository.GetByRoleAsync(request.Role, cancellationToken);

        if (existing == null)
        {
            var created = new RolePermissionSet(request.Role, request.Permissions);
            await _rolePermissionRepository.InsertAsync(created, cancellationToken);
            return new RolePermissionDto(created.Role, created.Permissions);
        }

        existing.ReplacePermissions(request.Permissions);
        await _rolePermissionRepository.UpdateAsync(existing, cancellationToken);
        return new RolePermissionDto(existing.Role, existing.Permissions);
    }
}
