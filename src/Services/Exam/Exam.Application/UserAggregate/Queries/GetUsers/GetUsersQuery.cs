using MediatR;

namespace Exam.Application.UserAggregate.Queries.GetUsers;

public record GetUsersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<UserDto>>;
