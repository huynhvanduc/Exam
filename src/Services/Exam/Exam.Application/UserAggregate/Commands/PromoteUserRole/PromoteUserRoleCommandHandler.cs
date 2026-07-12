using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.PromoteUserRole;

public class PromoteUserRoleCommandHandler : IRequestHandler<PromoteUserRoleCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public PromoteUserRoleCommandHandler(IUserRepository userRepository, IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<UserDto> Handle(PromoteUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(request.ExternalId, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), request.ExternalId);

        if (user.ExternalId == request.Actor.UserId)
            throw new ForbiddenException("You cannot change your own role. Ask another Admin to do it.");

        var oldRole = user.Role;

        user.ChangeRole(request.Role);

        await _userRepository.UpdateAsync(user, cancellationToken);

        var auditEntry = new AuditLogEntry(
            request.Actor.UserId,
            "User.RoleChanged",
            user.ExternalId,
            $"Changed role from {oldRole} to {request.Role}.");
        await _auditLogRepository.InsertAsync(auditEntry, cancellationToken);

        return UserMapper.ToDto(user);
    }
}
