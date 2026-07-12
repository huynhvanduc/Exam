using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Commands.RecordAnswer;

public class RecordAnswerCommandHandler : IRequestHandler<RecordAnswerCommand, RecordAnswerResultDto>
{
    private readonly IExamResultRepository _examResultRepository;
    private readonly ExamResultGradingService _gradingService;

    public RecordAnswerCommandHandler(IExamResultRepository examResultRepository, ExamResultGradingService gradingService)
    {
        _examResultRepository = examResultRepository;
        _gradingService = gradingService;
    }

    public async Task<RecordAnswerResultDto> Handle(RecordAnswerCommand request, CancellationToken cancellationToken)
    {
        var examResult = await _examResultRepository.GetByIdAsync(request.ExamResultId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ExamResult), request.ExamResultId);

        if (examResult.UserId != request.UserId)
            throw new ExamDomainException("This exam attempt does not belong to the current user.");

        if (examResult.IsExpired(DateTime.UtcNow))
        {
            await _gradingService.GradeAndFinishAsync(examResult, cancellationToken);
            await _examResultRepository.UpdateAsync(examResult, cancellationToken);
            return new RecordAnswerResultDto(Finished: true);
        }

        examResult.RecordAnswer(request.QuestionId, request.SelectedAnswerIds);

        await _examResultRepository.UpdateAsync(examResult, cancellationToken);

        return new RecordAnswerResultDto(Finished: false);
    }
}
