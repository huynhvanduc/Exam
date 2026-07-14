namespace Exam.Contracts;

public record QuestionDto(
    string Id,
    string Content,
    QuestionType QuestionType,
    Level Level,
    string CategoryId,
    string CategoryName,
    IReadOnlyCollection<AnswerDto> Answers,
    string Explain,
    string OwnerUserId);

public record AnswerDto(string Id, string Content, bool IsCorrect);
