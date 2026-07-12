using Exam.Contracts;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.PromoteUserRole;

public record PromoteUserRoleCommand(string ExternalId, UserRole Role, Actor Actor) : IRequest<UserDto>;
