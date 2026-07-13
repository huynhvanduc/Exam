namespace Exam.Contracts;

public record ImportQuestionsResultDto(int TotalRows, int SuccessCount, IReadOnlyCollection<ImportQuestionRowError> Errors);

public record ImportQuestionRowError(int RowNumber, string Message);
