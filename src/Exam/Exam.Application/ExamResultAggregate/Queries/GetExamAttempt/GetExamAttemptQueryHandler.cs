using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamAttempt;

public class GetExamAttemptQueryHandler : IRequestHandler<GetExamAttemptQuery, ExamAttemptStatusDto>
{
    private readonly IExamResultRepository _examResultRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly ExamResultGradingService _gradingService;

    public GetExamAttemptQueryHandler(IExamResultRepository examResultRepository, IQuestionRepository questionRepository,
        ExamResultGradingService gradingService)
    {
        _examResultRepository = examResultRepository;
        _questionRepository = questionRepository;
        _gradingService = gradingService;
    }

    public async Task<ExamAttemptStatusDto> Handle(GetExamAttemptQuery request, CancellationToken cancellationToken)
    {
        var examResult = await _examResultRepository.GetByIdAsync(request.ExamResultId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ExamResult), request.ExamResultId);

        if (examResult.UserId != request.UserId)
            throw new ExamDomainException("This exam attempt does not belong to the current user.");

        if (examResult.IsExpired(DateTime.UtcNow))
        {
            await _gradingService.GradeAndFinishAsync(examResult, cancellationToken);
            await _examResultRepository.UpdateAsync(examResult, cancellationToken);
        }

        if (examResult.Finished)
            return new ExamAttemptStatusDto(true, null, ExamResultMapper.ToResultDto(examResult));

        var questions = await _questionRepository.GetByIdsAsync(examResult.QuestionIds, cancellationToken);

        return new ExamAttemptStatusDto(false, ExamResultMapper.ToAttemptDto(examResult, questions), null);
    }
}
