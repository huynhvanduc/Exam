using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.Services;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Commands.AdminForceFinishExam;

// Bản admin-scoped của FinishExamCommandHandler - bỏ kiểm tra sở hữu (thí sinh không tự nộp bài,
// admin buộc nộp hộ), tái dùng cùng ExamResultGradingService để đảm bảo cách chấm điểm giống hệt
// luồng thí sinh tự nộp. Endpoint gọi handler này tự gate quyền qua policy Exam.ForceFinishAttempt.
public class AdminForceFinishExamCommandHandler : IRequestHandler<AdminForceFinishExamCommand, ExamResultDto>
{
    private readonly IExamResultRepository _examResultRepository;
    private readonly ExamResultGradingService _gradingService;

    public AdminForceFinishExamCommandHandler(IExamResultRepository examResultRepository, ExamResultGradingService gradingService)
    {
        _examResultRepository = examResultRepository;
        _gradingService = gradingService;
    }

    public async Task<ExamResultDto> Handle(AdminForceFinishExamCommand request, CancellationToken cancellationToken)
    {
        var examResult = await _examResultRepository.GetByIdAsync(request.ExamResultId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ExamResult), request.ExamResultId);

        await _gradingService.GradeAndFinishAsync(examResult, cancellationToken);

        await _examResultRepository.UpdateAsync(examResult, cancellationToken);

        return ExamResultMapper.ToResultDto(examResult);
    }
}
