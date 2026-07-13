using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamResultsByExam;

public class GetExamResultsByExamQueryHandler : IRequestHandler<GetExamResultsByExamQuery, PagedResult<ExamResultAdminListItemDto>>
{
    private readonly IExamRepository _examRepository;
    private readonly IExamResultRepository _examResultRepository;

    public GetExamResultsByExamQueryHandler(IExamRepository examRepository, IExamResultRepository examResultRepository)
    {
        _examRepository = examRepository;
        _examResultRepository = examResultRepository;
    }

    public async Task<PagedResult<ExamResultAdminListItemDto>> Handle(GetExamResultsByExamQuery request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        // Điểm số + thông tin cá nhân (email, họ tên) của học viên chỉ nên xem được bởi chủ đề thi hoặc
        // Admin - trước đây endpoint này chỉ kiểm tra quyền Exam.ViewResults (mọi giáo viên đều có sẵn),
        // không kiểm tra ai là chủ đề thi, khiến giáo viên xem được kết quả đề thi của giáo viên khác.
        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, exam.OwnerUserId, nameof(Domain.AggregateModels.ExamAggregate.Exam), exam.Id);

        return await PagedResultFactory.CreateAsync<ExamResultAdminListItemDto>(request.Page, request.PageSize,
            async (skip, take) => (await _examResultRepository.GetByExamIdAsync(request.ExamId, skip, take, cancellationToken))
                .Select(ToDto)
                .ToList(),
            () => _examResultRepository.CountByExamIdAsync(request.ExamId, cancellationToken));
    }

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
