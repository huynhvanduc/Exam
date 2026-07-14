using Exam.Contracts;
using MediatR;

namespace Exam.Application.UserAggregate.Queries.GetUsers;

public record GetUsersQuery(int Page = 1, int PageSize = 20, string? Search = null, UserRole? Role = null,
    bool? IsActive = null) : IRequest<PagedResult<UserDto>>;
