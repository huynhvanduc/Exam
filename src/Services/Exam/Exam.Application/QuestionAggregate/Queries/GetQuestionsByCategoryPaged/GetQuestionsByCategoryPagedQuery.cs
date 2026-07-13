using MediatR;

namespace Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategoryPaged;

public record GetQuestionsByCategoryPagedQuery(
    string CategoryId,
    int Page = 1,
    int PageSize = 20,
    Level? Level = null,
    QuestionType? QuestionType = null,
    string? Keyword = null) : IRequest<PagedResult<QuestionDto>>;
