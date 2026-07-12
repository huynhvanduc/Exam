using Exam.Contracts;

namespace Exam.Application.ExamResultAggregate;

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
    int Points,
    IReadOnlyCollection<ExamAttemptAnswerOptionDto> Answers);

public record ExamAttemptAnswerOptionDto(string Id, string Content);

public record ExamAttemptAnswerSelectionDto(string QuestionId, IReadOnlyCollection<string> SelectedAnswerIds);
