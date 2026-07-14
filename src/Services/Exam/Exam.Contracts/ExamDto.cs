namespace Exam.Contracts;

public record ExamCompositionCellDto(Level Level, QuestionType QuestionType, int Count);

public record ExamDto(
    string Id,
    string Name,
    string ShortDesc,
    string Content,
    TimeSpan Duration,
    Level Level,
    DateTime DateCreated,
    string OwnerUserId,
    decimal MinimumPassingScore,
    bool IsTimeRestricted,
    string CategoryId,
    string CategoryName,
    ExamStatus Status,
    IReadOnlyCollection<ExamCompositionCellDto> Composition,
    DateTime? AvailableFrom,
    DateTime? AvailableTo,
    int NumberOfQuestions,
    IReadOnlyCollection<string> AssignedClassIds,
    bool IsPublic,
    int? MaxAttempts);
