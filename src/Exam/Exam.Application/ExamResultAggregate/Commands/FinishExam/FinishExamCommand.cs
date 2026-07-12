using MediatR;

namespace Exam.Application.ExamResultAggregate.Commands.FinishExam;

public record FinishExamCommand(string ExamResultId, string UserId) : IRequest<ExamResultDto>;
