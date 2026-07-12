using MediatR;

namespace Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategoryPaged;

public record GetQuestionsByCategoryPagedQuery(string CategoryId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<QuestionDto>>;
