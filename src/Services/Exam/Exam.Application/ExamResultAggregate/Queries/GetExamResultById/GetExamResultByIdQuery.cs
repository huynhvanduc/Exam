using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamResultById;

public record GetExamResultByIdQuery(string ExamResultId, string UserId) : IRequest<ExamResultDto?>;
