namespace Exam.Contracts;

public record ExamRequest(
    string Name,
    string ShortDesc,
    string Content,
    TimeSpan Duration,
    Level Level,
    string CategoryId,
    bool IsTimeRestricted,
    decimal MinimumPassingScore);

public record ConfigureExamCompositionRequest(IReadOnlyCollection<ExamCompositionCellDto> Cells);

public record ScheduleExamAvailabilityRequest(DateTime? AvailableFrom, DateTime? AvailableTo);

public record ConfigureMaxAttemptsRequest(int? MaxAttempts);
