using Exam.Application.Common;
using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Queries.GetAvailableExams;

public class GetAvailableExamsQueryHandler : IRequestHandler<GetAvailableExamsQuery, PagedResult<ExamDto>>
{
    private readonly IExamRepository _examRepository;

    public GetAvailableExamsQueryHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public Task<PagedResult<ExamDto>> Handle(GetAvailableExamsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return PagedResultFactory.CreateAsync<ExamDto>(request.Page, request.PageSize,
            async (skip, take) => (await _examRepository.GetAvailableAsync(now, skip, take, cancellationToken)).Select(ExamMapper.ToDto).ToList(),
            () => _examRepository.CountAvailableAsync(now, cancellationToken));
    }
}
