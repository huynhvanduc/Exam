using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Contracts;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.EnsureUserProvisioned;

public class EnsureUserProvisionedCommandHandler : IRequestHandler<EnsureUserProvisionedCommand, UserDto>
{
    private readonly IUserRepository _userRepository;

    public EnsureUserProvisionedCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(EnsureUserProvisionedCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByExternalIdAsync(request.ExternalId, cancellationToken);
        if (existing != null)
            return UserMapper.ToDto(existing);

        var firstName = string.IsNullOrWhiteSpace(request.FirstName) ? "Unknown" : request.FirstName;
        var lastName = string.IsNullOrWhiteSpace(request.LastName) ? "User" : request.LastName;

        // Chưa có ai trong hệ thống -> người đăng nhập đầu tiên tự động là Admin (bootstrap),
        // tránh tình trạng không ai có quyền promote user khác.
        var isFirstUser = !await _userRepository.AnyAsync(cancellationToken);
        var role = isFirstUser ? UserRole.Admin : UserRole.Student;

        var user = User.CreateNewUser(request.ExternalId, firstName, lastName, role);

        await _userRepository.InsertAsync(user, cancellationToken);

        return UserMapper.ToDto(user);
    }
}
