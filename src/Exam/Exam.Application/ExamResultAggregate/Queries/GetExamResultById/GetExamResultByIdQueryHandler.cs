using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.Exceptions;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamResultById;

public class GetExamResultByIdQueryHandler : IRequestHandler<GetExamResultByIdQuery, ExamResultDto?>
{
    private readonly IExamResultRepository _examResultRepository;

    public GetExamResultByIdQueryHandler(IExamResultRepository examResultRepository)
    {
        _examResultRepository = examResultRepository;
    }

    public async Task<ExamResultDto?> Handle(GetExamResultByIdQuery request, CancellationToken cancellationToken)
    {
        var examResult = await _examResultRepository.GetByIdAsync(request.ExamResultId, cancellationToken);

        if (examResult == null)
            return null;

        if (examResult.UserId != request.UserId)
            throw new ExamDomainException("This exam attempt does not belong to the current user.");

        return examResult.Finished ? ExamResultMapper.ToResultDto(examResult) : null;
    }
}
