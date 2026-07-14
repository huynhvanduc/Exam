namespace Exam.Contracts;

public record ExamAttemptDto(
    string Id,
    string ExamId,
    string ExamTitle,
    DateTime ExamStartDate,
    DateTime? Deadline,
    IReadOnlyCollection<ExamAttemptQuestionDto> Questions,
    IReadOnlyCollection<ExamAttemptAnswerSelectionDto> SelectedAnswers);

public record ExamAttemptQuestionDto(
    string Id,
    string Content,
    QuestionType QuestionType,
    Level Level,
    IReadOnlyCollection<ExamAttemptAnswerOptionDto> Answers);

public record ExamAttemptAnswerOptionDto(string Id, string Content);

public record ExamAttemptAnswerSelectionDto(string QuestionId, IReadOnlyCollection<string> SelectedAnswerIds);

public record ExamAttemptStatusDto(bool Finished, ExamAttemptDto? Attempt, ExamResultDto? Result);
