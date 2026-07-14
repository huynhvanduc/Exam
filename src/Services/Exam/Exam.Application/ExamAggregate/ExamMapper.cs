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
            exam.Composition.Select(c => new ExamCompositionCellDto(c.Level, c.QuestionType, c.Count)).ToList(),
            exam.AvailableFrom,
            exam.AvailableTo,
            exam.NumberOfQuestions,
            exam.AssignedClassIds,
            exam.IsPublic,
            exam.MaxAttempts);
}
