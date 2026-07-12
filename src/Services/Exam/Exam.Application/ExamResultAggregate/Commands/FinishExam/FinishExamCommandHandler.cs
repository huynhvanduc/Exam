using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Commands.FinishExam;

public class FinishExamCommandHandler : IRequestHandler<FinishExamCommand, ExamResultDto>
{
    private readonly IExamResultRepository _examResultRepository;
    private readonly ExamResultGradingService _gradingService;

    public FinishExamCommandHandler(IExamResultRepository examResultRepository, ExamResultGradingService gradingService)
    {
        _examResultRepository = examResultRepository;
        _gradingService = gradingService;
    }

    public async Task<ExamResultDto> Handle(FinishExamCommand request, CancellationToken cancellationToken)
    {
        var examResult = await _examResultRepository.GetByIdAsync(request.ExamResultId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ExamResult), request.ExamResultId);

        if (examResult.UserId != request.UserId)
            throw new ExamDomainException("This exam attempt does not belong to the current user.");

        await _gradingService.GradeAndFinishAsync(examResult, cancellationToken);

        await _examResultRepository.UpdateAsync(examResult, cancellationToken);

        return ExamResultMapper.ToResultDto(examResult);
    }
}
