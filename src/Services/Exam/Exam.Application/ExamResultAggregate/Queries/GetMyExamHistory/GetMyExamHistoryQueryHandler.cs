using Exam.Domain.AggregateModels.ExamResultAggregate;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetMyExamHistory;

public class GetMyExamHistoryQueryHandler : IRequestHandler<GetMyExamHistoryQuery, PagedResult<ExamResultSummaryDto>>
{
    private readonly IExamResultRepository _examResultRepository;

    public GetMyExamHistoryQueryHandler(IExamResultRepository examResultRepository)
    {
        _examResultRepository = examResultRepository;
    }

    public Task<PagedResult<ExamResultSummaryDto>> Handle(GetMyExamHistoryQuery request, CancellationToken cancellationToken) =>
        PagedResultFactory.CreateAsync<ExamResultSummaryDto>(request.Page, request.PageSize,
            async (skip, take) => (await _examResultRepository.GetByUserIdAsync(request.UserId, skip, take, cancellationToken)).Select(ExamResultMapper.ToSummaryDto).ToList(),
            () => _examResultRepository.CountByUserIdAsync(request.UserId, cancellationToken));
}
