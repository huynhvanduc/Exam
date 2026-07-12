using MediatR;

namespace Exam.Application.ExamResultAggregate.Commands.StartExam;

public record StartExamCommand(string ExamId, string UserId) : IRequest<ExamAttemptDto>;
