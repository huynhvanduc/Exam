using Exam.Domain.AggregateModels.ExamResultAggregate;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetMyExamHistory;

public class GetMyExamHistoryQueryHandler : IRequestHandler<GetMyExamHistoryQuery, IReadOnlyCollection<ExamResultSummaryDto>>
{
    private readonly IExamResultRepository _examResultRepository;

    public GetMyExamHistoryQueryHandler(IExamResultRepository examResultRepository)
    {
        _examResultRepository = examResultRepository;
    }

    public async Task<IReadOnlyCollection<ExamResultSummaryDto>> Handle(GetMyExamHistoryQuery request, CancellationToken cancellationToken)
    {
        var results = await _examResultRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return results.Select(ExamResultMapper.ToSummaryDto).ToList();
    }
}
