using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Services;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamAttemptAdminStatus;

// Bản admin-scoped của GetExamAttemptQueryHandler - bỏ kiểm tra "examResult.UserId != request.UserId"
// (admin được phép xem tiến độ bài thi của BẤT KỲ thí sinh nào), giữ nguyên phần load câu hỏi + chấm
// điểm tự động khi hết giờ. Endpoint gọi handler này tự gate quyền qua policy Exam.ViewResults.
public class GetExamAttemptAdminStatusQueryHandler : IRequestHandler<GetExamAttemptAdminStatusQuery, ExamAttemptStatusDto>
{
    private readonly IExamResultRepository _examResultRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly ExamResultGradingService _gradingService;

    public GetExamAttemptAdminStatusQueryHandler(IExamResultRepository examResultRepository, IQuestionRepository questionRepository,
        ExamResultGradingService gradingService)
    {
        _examResultRepository = examResultRepository;
        _questionRepository = questionRepository;
        _gradingService = gradingService;
    }

    public async Task<ExamAttemptStatusDto> Handle(GetExamAttemptAdminStatusQuery request, CancellationToken cancellationToken)
    {
        var examResult = await _examResultRepository.GetByIdAsync(request.ExamResultId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ExamResult), request.ExamResultId);

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
