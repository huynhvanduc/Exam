namespace Exam.Contracts;

public record ClassRoomDto(string Id, string Name, string JoinCode, string OwnerUserId, DateTime DateCreated, int MemberCount);

public record ClassMemberDto(string UserId, string FullName, string Email);

public record ClassRoomDetailDto(
    string Id,
    string Name,
    string JoinCode,
    string OwnerUserId,
    DateTime DateCreated,
    IReadOnlyCollection<ClassMemberDto> Members);
