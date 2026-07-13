namespace Exam.Contracts;

public record StartExamRequest(string ExamId);

public record RecordAnswerRequest(string QuestionId, IReadOnlyCollection<string> SelectedAnswerIds);

public record RecordAnswerResultDto(bool Finished);
