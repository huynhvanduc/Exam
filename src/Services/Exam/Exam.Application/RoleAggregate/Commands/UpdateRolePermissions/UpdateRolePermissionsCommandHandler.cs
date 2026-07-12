using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.RoleAggregate;
using Exam.Domain.Exceptions;
using MediatR;

namespace Exam.Application.RoleAggregate.Commands.UpdateRolePermissions;

public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, RolePermissionDto>
{
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public UpdateRolePermissionsCommandHandler(IRolePermissionRepository rolePermissionRepository, IAuditLogRepository auditLogRepository)
    {
        _rolePermissionRepository = rolePermissionRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<RolePermissionDto> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        if (request.Role == UserRole.Admin)
            throw new ExamDomainException("Admin always has full access; its permission set cannot be changed.");

        var existing = await _rolePermissionRepository.GetByRoleAsync(request.Role, cancellationToken);
        var oldPermissions = existing?.Permissions ?? [];

        RolePermissionDto result;

        if (existing == null)
        {
            var created = new RolePermissionSet(request.Role, request.Permissions);
            await _rolePermissionRepository.InsertAsync(created, cancellationToken);
            result = new RolePermissionDto(created.Role, created.Permissions);
        }
        else
        {
            existing.ReplacePermissions(request.Permissions);
            await _rolePermissionRepository.UpdateAsync(existing, cancellationToken);
            result = new RolePermissionDto(existing.Role, existing.Permissions);
        }

        var added = request.Permissions.Except(oldPermissions).ToList();
        var removed = oldPermissions.Except(request.Permissions).ToList();

        if (added.Count > 0 || removed.Count > 0)
        {
            var description = $"Granted: {(added.Count > 0 ? string.Join(", ", added) : "(none)")}. " +
                               $"Revoked: {(removed.Count > 0 ? string.Join(", ", removed) : "(none)")}.";
            var auditEntry = new AuditLogEntry(request.Actor.UserId, "Role.PermissionsChanged", request.Role.ToString(), description);
            await _auditLogRepository.InsertAsync(auditEntry, cancellationToken);
        }

        return result;
    }
}
