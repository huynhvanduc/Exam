using MediatR;

namespace Exam.Application.ExamAggregate.Queries.GetExamsByCategory;

public record GetExamsByCategoryQuery(string CategoryId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<ExamDto>>;
