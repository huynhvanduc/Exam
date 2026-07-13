namespace Exam.Contracts;

public record ExamRequest(
    string Name,
    string ShortDesc,
    string Content,
    TimeSpan Duration,
    Level Level,
    string CategoryId,
    bool IsTimeRestricted,
    int MinimumPassingScore);

public record ConfigureQuestionPoolRequest(string PoolCategoryId, int PoolQuestionCount);

public record ScheduleExamAvailabilityRequest(DateTime? AvailableFrom, DateTime? AvailableTo);

public record ConfigureNegativeMarkingRequest(decimal Ratio);

public record ConfigureMaxAttemptsRequest(int? MaxAttempts);
