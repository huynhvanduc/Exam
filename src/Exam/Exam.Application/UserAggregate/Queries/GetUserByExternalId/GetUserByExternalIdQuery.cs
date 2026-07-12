using MediatR;

namespace Exam.Application.UserAggregate.Queries.GetUserByExternalId;

public record GetUserByExternalIdQuery(string ExternalId) : IRequest<UserDto?>;
