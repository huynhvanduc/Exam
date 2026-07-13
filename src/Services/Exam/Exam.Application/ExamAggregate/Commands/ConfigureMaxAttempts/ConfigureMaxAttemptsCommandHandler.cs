using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ConfigureMaxAttempts;

public class ConfigureMaxAttemptsCommandHandler : IRequestHandler<ConfigureMaxAttemptsCommand, ExamDto>
{
    private readonly IExamRepository _examRepository;

    public ConfigureMaxAttemptsCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<ExamDto> Handle(ConfigureMaxAttemptsCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, exam.OwnerUserId, nameof(Domain.AggregateModels.ExamAggregate.Exam), exam.Id);

        exam.ConfigureMaxAttempts(request.MaxAttempts);

        await _examRepository.UpdateAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
