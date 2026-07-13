using Exam.Application.Common;
using Exam.Contracts;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.ToggleUserActive;

// Đã tự ghi audit log chi tiết (khoá/mở khoá) trong handler -> bỏ qua AuditLoggingBehavior.
public record ToggleUserActiveCommand(string ExternalId, bool IsActive, Actor Actor)
    : IRequest<UserDto>, ISkipAutoAuditLog;
