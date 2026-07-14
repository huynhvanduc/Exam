namespace Exam.Contracts;

public record ImportQuestionsResultDto(
    int TotalRows,
    int SuccessCount,
    IReadOnlyCollection<ImportQuestionRowError> Errors,
    IReadOnlyCollection<ImportQuestionPreviewRow> ValidRows);

public record ImportQuestionRowError(int RowNumber, string Message, string ContentExcerpt);

public record ImportQuestionPreviewRow(int RowNumber, string CategoryName, string Content, string QuestionType, string Level);
