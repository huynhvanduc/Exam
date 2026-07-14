namespace Exam.Contracts;

public record MoveQuestionsRequest(IReadOnlyCollection<string> QuestionIds, string TargetCategoryId);
