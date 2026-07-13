namespace Exam.Application.ExamAggregate;

internal static class ExamMapper
{
    public static ExamDto ToDto(Domain.AggregateModels.ExamAggregate.Exam exam) =>
        new(
            exam.Id,
            exam.Name,
            exam.ShortDesc,
            exam.Content,
            exam.Duration,
            exam.Level,
            exam.DateCreated,
            exam.OwnerUserId,
            exam.MinimumPassingScore,
            exam.IsTimeRestricted,
            exam.CategoryId,
            exam.CategoryName,
            exam.Status,
            exam.QuestionSelectionMode,
            exam.PoolCategoryId,
            exam.PoolQuestionCount,
            exam.AvailableFrom,
            exam.AvailableTo,
            exam.NegativeMarkingRatio,
            exam.QuestionIds,
            exam.NumberOfQuestions,
            exam.AssignedClassIds,
            exam.IsPublic,
            exam.MaxAttempts);
}
