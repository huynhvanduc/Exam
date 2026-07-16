using Exam.Application.Exceptions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Domain.Services;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.ResetUserPassword;

public class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, ResetUserPasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityAccountProvisioningService _identityAccountProvisioningService;

    public ResetUserPasswordCommandHandler(IUserRepository userRepository, IIdentityAccountProvisioningService identityAccountProvisioningService)
    {
        _userRepository = userRepository;
        _identityAccountProvisioningService = identityAccountProvisioningService;
    }

    public async Task<ResetUserPasswordResponse> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(request.ExternalId, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), request.ExternalId);

        if (user.ExternalId == request.Actor.UserId)
            throw new ForbiddenException("Không thể tự đặt lại mật khẩu của chính mình bằng thao tác này - dùng chức năng Đổi mật khẩu.");

        var generatedPassword = await _identityAccountProvisioningService.ResetPasswordAsync(request.ExternalId, cancellationToken);

        return new ResetUserPasswordResponse(request.ExternalId, generatedPassword);
    }
}
