using Exam.Application.Exceptions;
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

        // Trước đây chỉ chủ lớp/Admin xem được chi tiết lớp (kể cả danh sách thành viên) - học viên là
        // thành viên lớp không có cách nào thấy mình đang học cùng ai. Cho phép thêm: bất kỳ thành viên nào
        // của lớp cũng xem được (chỉ xem, các API sửa/xoá/quản lý thành viên khác vẫn yêu cầu chủ lớp/Admin).
        var isAllowed = request.Actor.Role == UserRole.Admin
            || request.Actor.UserId == classRoom.OwnerUserId
            || classRoom.HasMember(request.Actor.UserId);

        if (!isAllowed)
            throw ForbiddenException.NotOwner(nameof(ClassRoom), classRoom.Id);

        var members = await _userRepository.GetByExternalIdsAsync(classRoom.MemberUserIds, cancellationToken);

        return ClassRoomMapper.ToDetailDto(classRoom, members);
    }
}
