using MediatR;

namespace Exam.Application.UserAggregate.Commands.EnsureUserProvisioned;

public record EnsureUserProvisionedCommand(string ExternalId, string Email, string FirstName, string LastName) : IRequest<UserDto>;
