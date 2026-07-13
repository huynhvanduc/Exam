using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ClassAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Commands.RegenerateJoinCode;

public class RegenerateJoinCodeCommandHandler : IRequestHandler<RegenerateJoinCodeCommand, ClassRoomDto>
{
    private readonly IClassRoomRepository _classRoomRepository;

    public RegenerateJoinCodeCommandHandler(IClassRoomRepository classRoomRepository)
    {
        _classRoomRepository = classRoomRepository;
    }

    public async Task<ClassRoomDto> Handle(RegenerateJoinCodeCommand request, CancellationToken cancellationToken)
    {
        var classRoom = await _classRoomRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(ClassRoom), request.Id);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, classRoom.OwnerUserId, nameof(ClassRoom), classRoom.Id);

        classRoom.RegenerateJoinCode();

        await _classRoomRepository.UpdateAsync(classRoom, cancellationToken);

        return ClassRoomMapper.ToDto(classRoom);
    }
}
