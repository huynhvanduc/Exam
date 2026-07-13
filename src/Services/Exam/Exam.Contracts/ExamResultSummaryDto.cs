namespace Exam.Contracts;

public record ExamResultSummaryDto(
    string Id,
    string ExamId,
    string ExamTitle,
    decimal TotalScore,
    int MaxPossibleScore,
    bool? Passed,
    DateTime ExamStartDate,
    DateTime? ExamFinishDate,
    bool Finished);
