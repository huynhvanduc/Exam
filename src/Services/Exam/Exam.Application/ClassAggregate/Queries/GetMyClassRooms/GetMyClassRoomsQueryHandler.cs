using Exam.Domain.AggregateModels.ClassAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Queries.GetMyClassRooms;

public class GetMyClassRoomsQueryHandler : IRequestHandler<GetMyClassRoomsQuery, IReadOnlyCollection<ClassRoomDto>>
{
    private readonly IClassRoomRepository _classRoomRepository;

    public GetMyClassRoomsQueryHandler(IClassRoomRepository classRoomRepository)
    {
        _classRoomRepository = classRoomRepository;
    }

    public async Task<IReadOnlyCollection<ClassRoomDto>> Handle(GetMyClassRoomsQuery request, CancellationToken cancellationToken)
    {
        var classRooms = await _classRoomRepository.GetByMemberAsync(request.UserId, cancellationToken);

        return classRooms.Select(ClassRoomMapper.ToDto).ToList();
    }
}
