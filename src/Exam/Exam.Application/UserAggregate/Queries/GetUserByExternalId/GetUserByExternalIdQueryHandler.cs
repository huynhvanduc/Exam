using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.UserAggregate.Queries.GetUserByExternalId;

public class GetUserByExternalIdQueryHandler : IRequestHandler<GetUserByExternalIdQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByExternalIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> Handle(GetUserByExternalIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(request.ExternalId, cancellationToken);

        return user == null ? null : UserMapper.ToDto(user);
    }
}
