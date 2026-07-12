using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.PublishExam;

public class PublishExamCommandHandler : IRequestHandler<PublishExamCommand, ExamDto>
{
    private readonly IExamRepository _examRepository;

    public PublishExamCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<ExamDto> Handle(PublishExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        exam.Publish();

        await _examRepository.UpdateAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
