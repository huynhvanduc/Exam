using Exam.Application.Common;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.EnsureUserProvisioned;

// Chạy trên MỌI request đã xác thực (UserProvisioningMiddleware) -> quá nhiều noise nếu audit log tự động.
public record EnsureUserProvisionedCommand(string ExternalId, string Email, string FirstName, string LastName)
    : IRequest<UserDto>, ISkipAutoAuditLog;
