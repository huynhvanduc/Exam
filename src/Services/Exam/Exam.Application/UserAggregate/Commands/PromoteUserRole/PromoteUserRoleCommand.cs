using Exam.Application.Common;
using Exam.Contracts;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.PromoteUserRole;

// Đã tự ghi audit log chi tiết (từ role cũ sang role mới) trong handler -> bỏ qua AuditLoggingBehavior.
public record PromoteUserRoleCommand(string ExternalId, UserRole Role, Actor Actor)
    : IRequest<UserDto>, ISkipAutoAuditLog;
