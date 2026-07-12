using MediatR;

namespace Exam.Application.ExamResultAggregate.Commands.RecordAnswer;

public record RecordAnswerCommand(
    string ExamResultId,
    string UserId,
    string QuestionId,
    IReadOnlyCollection<string> SelectedAnswerIds) : IRequest<RecordAnswerResultDto>;

public record RecordAnswerResultDto(bool Finished);
