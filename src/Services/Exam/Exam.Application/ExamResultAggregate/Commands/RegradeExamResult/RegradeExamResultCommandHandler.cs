using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.Services;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Commands.RegradeExamResult;

// Sửa gap "bài đã chấm nhưng đáp án bị đánh sai thì không có cách nào chấm lại": tái dùng
// ExamResultGradingService để tính lại QuestionResults + điểm số từ dữ liệu câu hỏi HIỆN TẠI trong
// ngân hàng (không phải bản chấm cũ), giữ nguyên ExamStartDate/ExamFinishDate. Endpoint tự gate quyền
// qua policy Exam.Regrade, không cần kiểm tra sở hữu ở đây (mirror AdminForceFinishExamCommandHandler).
public class RegradeExamResultCommandHandler : IRequestHandler<RegradeExamResultCommand, ExamResultDto>
{
    private readonly IExamResultRepository _examResultRepository;
    private readonly ExamResultGradingService _gradingService;

    public RegradeExamResultCommandHandler(IExamResultRepository examResultRepository, ExamResultGradingService gradingService)
    {
        _examResultRepository = examResultRepository;
        _gradingService = gradingService;
    }

    public async Task<ExamResultDto> Handle(RegradeExamResultCommand request, CancellationToken cancellationToken)
    {
        var examResult = await _examResultRepository.GetByIdAsync(request.ExamResultId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ExamResult), request.ExamResultId);

        await _gradingService.RegradeAsync(examResult, cancellationToken);

        await _examResultRepository.UpdateAsync(examResult, cancellationToken);

        return ExamResultMapper.ToResultDto(examResult);
    }
}
