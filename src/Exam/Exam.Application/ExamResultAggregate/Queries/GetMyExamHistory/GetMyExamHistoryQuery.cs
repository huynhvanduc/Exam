using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetMyExamHistory;

public record GetMyExamHistoryQuery(string UserId) : IRequest<IReadOnlyCollection<ExamResultSummaryDto>>;
