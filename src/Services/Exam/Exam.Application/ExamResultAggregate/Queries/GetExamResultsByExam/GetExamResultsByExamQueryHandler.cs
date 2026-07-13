using Exam.Domain.AggregateModels.ExamResultAggregate;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamResultsByExam;

public class GetExamResultsByExamQueryHandler : IRequestHandler<GetExamResultsByExamQuery, PagedResult<ExamResultAdminListItemDto>>
{
    private readonly IExamResultRepository _examResultRepository;

    public GetExamResultsByExamQueryHandler(IExamResultRepository examResultRepository)
    {
        _examResultRepository = examResultRepository;
    }

    public Task<PagedResult<ExamResultAdminListItemDto>> Handle(GetExamResultsByExamQuery request, CancellationToken cancellationToken) =>
        PagedResultFactory.CreateAsync<ExamResultAdminListItemDto>(request.Page, request.PageSize,
            async (skip, take) => (await _examResultRepository.GetByExamIdAsync(request.ExamId, skip, take, cancellationToken))
                .Select(ToDto)
                .ToList(),
            () => _examResultRepository.CountByExamIdAsync(request.ExamId, cancellationToken));

    private static ExamResultAdminListItemDto ToDto(ExamResult examResult) =>
        new(
            examResult.Id,
            examResult.UserId,
            examResult.Email,
            examResult.FullName,
            examResult.TotalScore,
            examResult.MaxPossibleScore,
            examResult.Passed,
            examResult.ExamStartDate,
            examResult.ExamFinishDate,
            examResult.Finished);
}
