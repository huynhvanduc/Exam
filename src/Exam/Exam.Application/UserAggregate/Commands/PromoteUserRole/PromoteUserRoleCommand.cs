using Exam.Domain.Enums;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.PromoteUserRole;

public record PromoteUserRoleCommand(string ExternalId, UserRole Role) : IRequest<UserDto>;
