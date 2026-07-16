using Exam.Application.UserAggregate;
using Exam.Contracts;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Domain.Services;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityAccountProvisioningService _identityAccountProvisioningService;

    public CreateUserCommandHandler(IUserRepository userRepository, IIdentityAccountProvisioningService identityAccountProvisioningService)
    {
        _userRepository = userRepository;
        _identityAccountProvisioningService = identityAccountProvisioningService;
    }

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var provisioned = await _identityAccountProvisioningService.CreateAccountAsync(
            request.Email, request.FirstName, request.LastName, cancellationToken);

        var user = User.CreateNewUser(provisioned.ExternalId, request.Email, request.FirstName, request.LastName, request.Role);
        await _userRepository.InsertAsync(user, cancellationToken);

        return new CreateUserResponse(UserMapper.ToDto(user), provisioned.GeneratedPassword);
    }
}
