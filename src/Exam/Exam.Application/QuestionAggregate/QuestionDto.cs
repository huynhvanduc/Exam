using Exam.Domain.Enums;

namespace Exam.Application.QuestionAggregate;

public record QuestionDto(
    string Id,
    string Content,
    QuestionType QuestionType,
    Level Level,
    string CategoryId,
    string CategoryName,
    IReadOnlyCollection<AnswerDto> Answers,
    string Explain,
    int Points,
    string OwnerUserId);

public record AnswerDto(string Id, string Content, bool IsCorrect);
