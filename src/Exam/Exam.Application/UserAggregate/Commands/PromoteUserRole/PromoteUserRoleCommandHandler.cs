using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.PromoteUserRole;

public class PromoteUserRoleCommandHandler : IRequestHandler<PromoteUserRoleCommand, UserDto>
{
    private readonly IUserRepository _userRepository;

    public PromoteUserRoleCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(PromoteUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(request.ExternalId, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), request.ExternalId);

        user.ChangeRole(request.Role);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return UserMapper.ToDto(user);
    }
}
