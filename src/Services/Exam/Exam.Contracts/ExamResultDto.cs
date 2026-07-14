namespace Exam.Contracts;

public record ExamResultDto(
    string Id,
    string ExamId,
    string ExamTitle,
    string UserId,
    string Email,
    string FullName,
    decimal TotalScore,
    decimal MaxPossibleScore,
    int CorrectQuestionCount,
    bool? Passed,
    DateTime ExamStartDate,
    DateTime? ExamFinishDate,
    bool Finished,
    IReadOnlyCollection<QuestionResultDto> QuestionResults);

public record QuestionResultDto(
    string Id,
    string Content,
    QuestionType QuestionType,
    Level Level,
    string Explain,
    bool Result,
    bool IsAnswered,
    IReadOnlyCollection<AnswerResultDto> Answers);

public record AnswerResultDto(string Id, string Content, bool? UserChosen, bool IsCorrect);
