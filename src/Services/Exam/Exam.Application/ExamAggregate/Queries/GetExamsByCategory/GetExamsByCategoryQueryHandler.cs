using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Queries.GetExamsByCategory;

public class GetExamsByCategoryQueryHandler : IRequestHandler<GetExamsByCategoryQuery, PagedResult<ExamDto>>
{
    private readonly IExamRepository _examRepository;

    public GetExamsByCategoryQueryHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public Task<PagedResult<ExamDto>> Handle(GetExamsByCategoryQuery request, CancellationToken cancellationToken) =>
        PagedResultFactory.CreateAsync<ExamDto>(request.Page, request.PageSize,
            async (skip, take) => (await _examRepository.GetByCategoryAsync(request.CategoryId, skip, take, cancellationToken)).Select(ExamMapper.ToDto).ToList(),
            () => _examRepository.CountByCategoryAsync(request.CategoryId, cancellationToken));
}
