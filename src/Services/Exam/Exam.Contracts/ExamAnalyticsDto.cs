namespace Exam.Contracts;

public record ExamAnalyticsDto(
    int TotalAttempts,
    decimal AverageScore,
    decimal PassRate,
    IReadOnlyCollection<ScoreHistogramBucketDto> ScoreHistogram,
    IReadOnlyCollection<HardestQuestionDto> HardestQuestions);

public record ScoreHistogramBucketDto(string RangeLabel, int Count);

public record HardestQuestionDto(string QuestionId, string Content, int TotalAnswered, int IncorrectCount, decimal IncorrectRate);
