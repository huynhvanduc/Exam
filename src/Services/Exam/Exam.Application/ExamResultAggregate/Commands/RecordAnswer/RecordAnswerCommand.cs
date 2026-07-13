using Exam.Application.Common;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Commands.RecordAnswer;

// Chạy nhiều lần trong 1 lượt thi (mỗi câu trả lời) -> quá nhiều noise nếu audit log tự động.
public record RecordAnswerCommand(
    string ExamResultId,
    string UserId,
    string QuestionId,
    IReadOnlyCollection<string> SelectedAnswerIds) : IRequest<RecordAnswerResultDto>, ISkipAutoAuditLog;
