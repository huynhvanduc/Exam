using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Queries.GetClassRoomById;

public class GetClassRoomByIdQueryHandler : IRequestHandler<GetClassRoomByIdQuery, ClassRoomDetailDto?>
{
    private readonly IClassRoomRepository _classRoomRepository;
    private readonly IUserRepository _userRepository;

    public GetClassRoomByIdQueryHandler(IClassRoomRepository classRoomRepository, IUserRepository userRepository)
    {
        _classRoomRepository = classRoomRepository;
        _userRepository = userRepository;
    }

    public async Task<ClassRoomDetailDto?> Handle(GetClassRoomByIdQuery request, CancellationToken cancellationToken)
    {
        var classRoom = await _classRoomRepository.GetByIdAsync(request.Id, cancellationToken);
        if (classRoom == null)
            return null;

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, classRoom.OwnerUserId, nameof(ClassRoom), classRoom.Id);

        var members = await _userRepository.GetByExternalIdsAsync(classRoom.MemberUserIds, cancellationToken);

        return ClassRoomMapper.ToDetailDto(classRoom, members);
    }
}
