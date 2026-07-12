using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Queries.GetExamById;

public class GetExamByIdQueryHandler : IRequestHandler<GetExamByIdQuery, ExamDto?>
{
    private readonly IExamRepository _examRepository;

    public GetExamByIdQueryHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<ExamDto?> Handle(GetExamByIdQuery request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.Id, cancellationToken);

        return exam == null ? null : ExamMapper.ToDto(exam);
    }
}
