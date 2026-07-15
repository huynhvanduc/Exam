namespace Exam.Contracts;

public record ImportClassMembersResultDto(
    int TotalRows,
    int AddedCount,
    int AlreadyMemberCount,
    IReadOnlyCollection<ImportClassMemberRowError> Errors,
    IReadOnlyCollection<ImportClassMemberPreviewRow> ValidRows);

public record ImportClassMemberRowError(int RowNumber, string Email, string Message);

public record ImportClassMemberPreviewRow(int RowNumber, string Email, string FullName, bool AlreadyMember);
