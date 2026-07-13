using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ClassAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Commands.DeleteClassRoom;

public class DeleteClassRoomCommandHandler : IRequestHandler<DeleteClassRoomCommand>
{
    private readonly IClassRoomRepository _classRoomRepository;

    public DeleteClassRoomCommandHandler(IClassRoomRepository classRoomRepository)
    {
        _classRoomRepository = classRoomRepository;
    }

    public async Task Handle(DeleteClassRoomCommand request, CancellationToken cancellationToken)
    {
        var classRoom = await _classRoomRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(ClassRoom), request.Id);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, classRoom.OwnerUserId, nameof(ClassRoom), classRoom.Id);

        await _classRoomRepository.DeleteAsync(classRoom.Id, cancellationToken);
    }
}
