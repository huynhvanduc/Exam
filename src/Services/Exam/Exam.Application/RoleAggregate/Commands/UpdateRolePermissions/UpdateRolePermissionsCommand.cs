using Exam.Application.Common;
using MediatR;

namespace Exam.Application.RoleAggregate.Commands.UpdateRolePermissions;

// Đã tự ghi audit log chi tiết (diff granted/revoked) trong handler -> bỏ qua AuditLoggingBehavior.
public record UpdateRolePermissionsCommand(UserRole Role, IReadOnlyCollection<string> Permissions, Actor Actor)
    : IRequest<RolePermissionDto>, ISkipAutoAuditLog;
