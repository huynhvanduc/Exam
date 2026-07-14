using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamAttemptAdminStatus;

public record GetExamAttemptAdminStatusQuery(string ExamResultId) : IRequest<ExamAttemptStatusDto>;
