using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Queries.GetExamsByCategory;

public class GetExamsByCategoryQueryHandler : IRequestHandler<GetExamsByCategoryQuery, IReadOnlyCollection<ExamDto>>
{
    private readonly IExamRepository _examRepository;

    public GetExamsByCategoryQueryHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<IReadOnlyCollection<ExamDto>> Handle(GetExamsByCategoryQuery request, CancellationToken cancellationToken)
    {
        var exams = await _examRepository.GetByCategoryAsync(request.CategoryId, cancellationToken);

        return exams.Select(ExamMapper.ToDto).ToList();
    }
}
