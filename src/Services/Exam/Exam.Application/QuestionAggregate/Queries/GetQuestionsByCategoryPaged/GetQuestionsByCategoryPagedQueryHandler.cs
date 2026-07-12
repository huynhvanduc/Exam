using Exam.Domain.AggregateModels.QuestionAggregate;
using MediatR;

namespace Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategoryPaged;

public class GetQuestionsByCategoryPagedQueryHandler : IRequestHandler<GetQuestionsByCategoryPagedQuery, PagedResult<QuestionDto>>
{
    private readonly IQuestionRepository _questionRepository;

    public GetQuestionsByCategoryPagedQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public Task<PagedResult<QuestionDto>> Handle(GetQuestionsByCategoryPagedQuery request, CancellationToken cancellationToken) =>
        PagedResultFactory.CreateAsync<QuestionDto>(request.Page, request.PageSize,
            async (skip, take) => (await _questionRepository.GetByCategoryAsync(request.CategoryId, skip, take, cancellationToken)).Select(QuestionMapper.ToDto).ToList(),
            () => _questionRepository.CountByCategoryAsync(request.CategoryId, cancellationToken));
}
