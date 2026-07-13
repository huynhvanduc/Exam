using Exam.Domain.AggregateModels.ClassAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Queries.GetAllClassRooms;

public class GetAllClassRoomsQueryHandler : IRequestHandler<GetAllClassRoomsQuery, IReadOnlyCollection<ClassRoomDto>>
{
    private readonly IClassRoomRepository _classRoomRepository;

    public GetAllClassRoomsQueryHandler(IClassRoomRepository classRoomRepository)
    {
        _classRoomRepository = classRoomRepository;
    }

    public async Task<IReadOnlyCollection<ClassRoomDto>> Handle(GetAllClassRoomsQuery request, CancellationToken cancellationToken)
    {
        // Instructor chỉ thấy lớp do chính mình tạo - tránh lộ danh sách học viên của lớp giáo viên khác quản lý.
        var classRooms = request.Actor.Role == UserRole.Admin
            ? await _classRoomRepository.GetAllAsync(cancellationToken)
            : await _classRoomRepository.GetByOwnerAsync(request.Actor.UserId, cancellationToken);

        return classRooms.Select(ClassRoomMapper.ToDto).ToList();
    }
}
