using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetMyExamHistory;

public record GetMyExamHistoryQuery(string UserId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<ExamResultSummaryDto>>;
