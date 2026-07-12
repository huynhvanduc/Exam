using MediatR;

namespace Exam.Application.UserAggregate.Queries.GetUsers;

public record GetUsersQuery : IRequest<IReadOnlyCollection<UserDto>>;
