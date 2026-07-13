using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.ClassAggregate;

internal static class ClassRoomMapper
{
    public static ClassRoomDto ToDto(ClassRoom classRoom) =>
        new(classRoom.Id, classRoom.Name, classRoom.JoinCode, classRoom.OwnerUserId, classRoom.DateCreated, classRoom.MemberUserIds.Count);

    public static ClassRoomDetailDto ToDetailDto(ClassRoom classRoom, IReadOnlyCollection<User> members)
    {
        var membersById = members.ToDictionary(m => m.ExternalId);

        var memberDtos = classRoom.MemberUserIds
            .Select(id => membersById.TryGetValue(id, out var user)
                ? new ClassMemberDto(id, $"{user.FirstName} {user.LastName}".Trim(), user.Email)
                : new ClassMemberDto(id, "(Không rõ)", string.Empty))
            .ToList();

        return new ClassRoomDetailDto(classRoom.Id, classRoom.Name, classRoom.JoinCode, classRoom.OwnerUserId, classRoom.DateCreated, memberDtos);
    }
}
