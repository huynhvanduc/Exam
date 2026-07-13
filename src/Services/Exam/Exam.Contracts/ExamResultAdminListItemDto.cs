namespace Exam.Contracts;

public record ExamResultAdminListItemDto(
    string Id,
    string UserId,
    string Email,
    string FullName,
    decimal TotalScore,
    int MaxPossibleScore,
    bool? Passed,
    DateTime ExamStartDate,
    DateTime? ExamFinishDate,
    bool Finished);
