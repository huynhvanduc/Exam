using Exam.Domain.AggregateModels.ClassAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Commands.CreateClassRoom;

public class CreateClassRoomCommandHandler : IRequestHandler<CreateClassRoomCommand, ClassRoomDto>
{
    private readonly IClassRoomRepository _classRoomRepository;

    public CreateClassRoomCommandHandler(IClassRoomRepository classRoomRepository)
    {
        _classRoomRepository = classRoomRepository;
    }

    public async Task<ClassRoomDto> Handle(CreateClassRoomCommand request, CancellationToken cancellationToken)
    {
        var classRoom = ClassRoom.Create(request.Name, request.OwnerUserId);

        await _classRoomRepository.InsertAsync(classRoom, cancellationToken);

        return ClassRoomMapper.ToDto(classRoom);
    }
}
