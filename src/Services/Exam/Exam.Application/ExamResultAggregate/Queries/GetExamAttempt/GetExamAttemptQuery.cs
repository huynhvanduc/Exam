using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamAttempt;

public record GetExamAttemptQuery(string ExamResultId, string UserId) : IRequest<ExamAttemptStatusDto>;

public record ExamAttemptStatusDto(bool Finished, ExamAttemptDto? Attempt, ExamResultDto? Result);
