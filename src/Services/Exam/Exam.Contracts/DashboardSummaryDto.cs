namespace Exam.Contracts;

public record DashboardSummaryDto(
    int TotalCategories,
    int TotalQuestions,
    int TotalExams,
    int PublishedExams,
    int TotalUsers,
    IReadOnlyCollection<AuditLogEntryDto> RecentActivity);
