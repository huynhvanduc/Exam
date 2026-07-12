namespace Exam.Contracts;

public record QuestionRequest(
    string Content,
    QuestionType QuestionType,
    Level Level,
    string CategoryId,
    IReadOnlyCollection<AnswerInput> Answers,
    string Explain,
    int Points);

public record AnswerInput(string Content, bool IsCorrect);
