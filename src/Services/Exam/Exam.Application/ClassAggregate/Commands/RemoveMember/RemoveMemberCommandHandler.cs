using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Commands.RemoveMember;

public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, ClassRoomDetailDto>
{
    private readonly IClassRoomRepository _classRoomRepository;
    private readonly IUserRepository _userRepository;

    public RemoveMemberCommandHandler(IClassRoomRepository classRoomRepository, IUserRepository userRepository)
    {
        _classRoomRepository = classRoomRepository;
        _userRepository = userRepository;
    }

    public async Task<ClassRoomDetailDto> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var classRoom = await _classRoomRepository.GetByIdAsync(request.ClassId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ClassRoom), request.ClassId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, classRoom.OwnerUserId, nameof(ClassRoom), classRoom.Id);

        classRoom.RemoveMember(request.UserId);

        await _classRoomRepository.UpdateAsync(classRoom, cancellationToken);

        var members = await _userRepository.GetByExternalIdsAsync(classRoom.MemberUserIds, cancellationToken);

        return ClassRoomMapper.ToDetailDto(classRoom, members);
    }
}
