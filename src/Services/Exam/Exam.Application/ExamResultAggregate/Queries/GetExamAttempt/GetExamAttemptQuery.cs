using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamAttempt;

public record GetExamAttemptQuery(string ExamResultId, string UserId) : IRequest<ExamAttemptStatusDto>;
