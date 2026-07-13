using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ClassAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Commands.RenameClassRoom;

public class RenameClassRoomCommandHandler : IRequestHandler<RenameClassRoomCommand, ClassRoomDto>
{
    private readonly IClassRoomRepository _classRoomRepository;

    public RenameClassRoomCommandHandler(IClassRoomRepository classRoomRepository)
    {
        _classRoomRepository = classRoomRepository;
    }

    public async Task<ClassRoomDto> Handle(RenameClassRoomCommand request, CancellationToken cancellationToken)
    {
        var classRoom = await _classRoomRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(ClassRoom), request.Id);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, classRoom.OwnerUserId, nameof(ClassRoom), classRoom.Id);

        classRoom.Rename(request.Name);

        await _classRoomRepository.UpdateAsync(classRoom, cancellationToken);

        return ClassRoomMapper.ToDto(classRoom);
    }
}
