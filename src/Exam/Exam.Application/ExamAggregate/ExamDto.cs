using Exam.Domain.Enums;

namespace Exam.Application.ExamAggregate;

public record ExamDto(
    string Id,
    string Name,
    string ShortDesc,
    string Content,
    TimeSpan Duration,
    Level Level,
    DateTime DateCreated,
    string OwnerUserId,
    int MinimumPassingScore,
    bool IsTimeRestricted,
    string CategoryId,
    string CategoryName,
    ExamStatus Status,
    QuestionSelectionMode QuestionSelectionMode,
    string PoolCategoryId,
    int PoolQuestionCount,
    DateTime? AvailableFrom,
    DateTime? AvailableTo,
    decimal NegativeMarkingRatio,
    IReadOnlyCollection<string> QuestionIds,
    int NumberOfQuestions);
