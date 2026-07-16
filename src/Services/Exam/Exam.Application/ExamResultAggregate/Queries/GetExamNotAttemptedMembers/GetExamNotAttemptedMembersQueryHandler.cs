using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamNotAttemptedMembers;

// Sửa gap "giảng viên không biết ai trong lớp chưa thi trước khi đề đóng": tính danh sách thành viên
// của (các) lớp được giao đề TRỪ đi những người đã có ít nhất 1 attempt (kể cả đang làm dở), để giảng
// viên nhắc nhở kịp thời trước khi hết hạn (AvailableTo).
public class GetExamNotAttemptedMembersQueryHandler : IRequestHandler<GetExamNotAttemptedMembersQuery, IReadOnlyCollection<ClassMemberDto>>
{
    private readonly IExamRepository _examRepository;
    private readonly IExamResultRepository _examResultRepository;
    private readonly IClassRoomRepository _classRoomRepository;
    private readonly IUserRepository _userRepository;

    public GetExamNotAttemptedMembersQueryHandler(IExamRepository examRepository, IExamResultRepository examResultRepository,
        IClassRoomRepository classRoomRepository, IUserRepository userRepository)
    {
        _examRepository = examRepository;
        _examResultRepository = examResultRepository;
        _classRoomRepository = classRoomRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<ClassMemberDto>> Handle(GetExamNotAttemptedMembersQuery request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ExamEntity), request.ExamId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, exam.OwnerUserId, nameof(ExamEntity), exam.Id);

        // Đề công khai (không giao lớp nào) không có danh sách cố định để biết ai "chưa thi" - chỉ áp dụng
        // cho đề đã giao riêng cho (các) lớp cụ thể.
        if (exam.IsPublic)
            return [];

        var memberIds = new HashSet<string>();
        foreach (var classId in exam.AssignedClassIds)
        {
            var classRoom = await _classRoomRepository.GetByIdAsync(classId, cancellationToken);
            if (classRoom == null)
                continue;

            foreach (var memberId in classRoom.MemberUserIds)
                memberIds.Add(memberId);
        }

        var attemptedUserIds = await _examResultRepository.GetAttemptedUserIdsByExamIdAsync(request.ExamId, cancellationToken);
        var notAttemptedIds = memberIds.Except(attemptedUserIds).ToList();

        if (notAttemptedIds.Count == 0)
            return [];

        var users = await _userRepository.GetByExternalIdsAsync(notAttemptedIds, cancellationToken);

        return users
            .Select(u => new ClassMemberDto(u.ExternalId, $"{u.FirstName} {u.LastName}".Trim(), u.Email))
            .OrderBy(m => m.FullName)
            .ToList();
    }
}
