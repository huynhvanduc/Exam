using Exam.Application.Common;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Queries.GetAvailableExams;

public class GetAvailableExamsQueryHandler : IRequestHandler<GetAvailableExamsQuery, PagedResult<ExamDto>>
{
    private readonly IExamRepository _examRepository;
    private readonly IClassRoomRepository _classRoomRepository;

    public GetAvailableExamsQueryHandler(IExamRepository examRepository, IClassRoomRepository classRoomRepository)
    {
        _examRepository = examRepository;
        _classRoomRepository = classRoomRepository;
    }

    public async Task<PagedResult<ExamDto>> Handle(GetAvailableExamsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var classIds = (await _classRoomRepository.GetByMemberAsync(request.UserId, cancellationToken))
            .Select(c => c.Id)
            .ToList();

        return await PagedResultFactory.CreateAsync<ExamDto>(request.Page, request.PageSize,
            async (skip, take) => (await _examRepository.GetAvailableForUserAsync(now, classIds, skip, take, cancellationToken)).Select(ExamMapper.ToDto).ToList(),
            () => _examRepository.CountAvailableForUserAsync(now, classIds, cancellationToken));
    }
}
