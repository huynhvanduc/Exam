using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.ToggleUserActive;

public class ToggleUserActiveCommandHandler : IRequestHandler<ToggleUserActiveCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ToggleUserActiveCommandHandler(IUserRepository userRepository, IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<UserDto> Handle(ToggleUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(request.ExternalId, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), request.ExternalId);

        if (user.ExternalId == request.Actor.UserId)
            throw new ForbiddenException("You cannot lock/unlock your own account. Ask another Admin to do it.");

        if (request.IsActive)
            user.Activate();
        else
            user.Deactivate();

        await _userRepository.UpdateAsync(user, cancellationToken);

        var auditEntry = new AuditLogEntry(
            request.Actor.UserId,
            "User.ActiveStatusChanged",
            user.ExternalId,
            request.IsActive ? "Unlocked account." : "Locked account.");
        await _auditLogRepository.InsertAsync(auditEntry, cancellationToken);

        return UserMapper.ToDto(user);
    }
}
