namespace Exam.Contracts;

public record DashboardSummaryDto(
    int TotalCategories,
    int TotalQuestions,
    int TotalExams,
    int PublishedExams,
    int TotalUsers,
    int TotalClasses,
    IReadOnlyCollection<AuditLogEntryDto> RecentActivity);
