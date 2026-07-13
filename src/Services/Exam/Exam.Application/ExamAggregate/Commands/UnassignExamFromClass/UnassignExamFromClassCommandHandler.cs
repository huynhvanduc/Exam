using Exam.Application.Exceptions;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.UnassignExamFromClass;

public class UnassignExamFromClassCommandHandler : IRequestHandler<UnassignExamFromClassCommand, ExamDto>
{
    private readonly Domain.AggregateModels.ExamAggregate.IExamRepository _examRepository;

    public UnassignExamFromClassCommandHandler(Domain.AggregateModels.ExamAggregate.IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<ExamDto> Handle(UnassignExamFromClassCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, exam.OwnerUserId, nameof(Domain.AggregateModels.ExamAggregate.Exam), exam.Id);

        exam.UnassignFromClass(request.ClassId);

        await _examRepository.UpdateAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
