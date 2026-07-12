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

    public async Task<PagedResult<ExamDto>> Handle(GetAvailableExamsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var skip = (request.Page - 1) * request.PageSize;

        var exams = await _examRepository.GetAvailableAsync(now, skip, request.PageSize, cancellationToken);
        var totalCount = await _examRepository.CountAvailableAsync(now, cancellationToken);

        return new PagedResult<ExamDto>(exams.Select(ExamMapper.ToDto).ToList(), request.Page, request.PageSize, totalCount);
    }
}
