using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.UnpublishExam;

public class UnpublishExamCommandHandler : IRequestHandler<UnpublishExamCommand, ExamDto>
{
    private readonly IExamRepository _examRepository;

    public UnpublishExamCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<ExamDto> Handle(UnpublishExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        exam.Unpublish();

        await _examRepository.UpdateAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
