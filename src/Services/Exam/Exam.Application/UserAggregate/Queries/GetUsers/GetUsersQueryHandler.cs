using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.UserAggregate.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken) =>
        PagedResultFactory.CreateAsync<UserDto>(request.Page, request.PageSize,
            async (skip, take) => (await _userRepository.GetPagedAsync(skip, take, request.Search, request.Role, request.IsActive, cancellationToken))
                .Select(UserMapper.ToDto).ToList(),
            () => _userRepository.CountAsync(request.Search, request.Role, request.IsActive, cancellationToken));
}
