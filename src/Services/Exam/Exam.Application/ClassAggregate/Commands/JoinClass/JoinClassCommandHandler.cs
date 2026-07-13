using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ClassAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Commands.JoinClass;

public class JoinClassCommandHandler : IRequestHandler<JoinClassCommand, ClassRoomDto>
{
    private readonly IClassRoomRepository _classRoomRepository;

    public JoinClassCommandHandler(IClassRoomRepository classRoomRepository)
    {
        _classRoomRepository = classRoomRepository;
    }

    public async Task<ClassRoomDto> Handle(JoinClassCommand request, CancellationToken cancellationToken)
    {
        var classRoom = await _classRoomRepository.GetByJoinCodeAsync(request.JoinCode.Trim().ToUpperInvariant(), cancellationToken)
            ?? throw NotFoundException.For(nameof(ClassRoom), request.JoinCode);

        classRoom.AddMember(request.UserId);

        await _classRoomRepository.UpdateAsync(classRoom, cancellationToken);

        return ClassRoomMapper.ToDto(classRoom);
    }
}
