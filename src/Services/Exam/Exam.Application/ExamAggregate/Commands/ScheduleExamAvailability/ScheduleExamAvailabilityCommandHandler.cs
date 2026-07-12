using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ScheduleExamAvailability;

public class ScheduleExamAvailabilityCommandHandler : IRequestHandler<ScheduleExamAvailabilityCommand, ExamDto>
{
    private readonly IExamRepository _examRepository;

    public ScheduleExamAvailabilityCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<ExamDto> Handle(ScheduleExamAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, exam.OwnerUserId, nameof(Domain.AggregateModels.ExamAggregate.Exam), exam.Id);

        exam.ScheduleAvailability(request.AvailableFrom, request.AvailableTo);

        await _examRepository.UpdateAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
