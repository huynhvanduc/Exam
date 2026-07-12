using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.DeleteExam;

public class DeleteExamCommandHandler : IRequestHandler<DeleteExamCommand>
{
    private readonly IExamRepository _examRepository;

    public DeleteExamCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task Handle(DeleteExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.Id);

        await _examRepository.DeleteAsync(exam.Id, cancellationToken);
    }
}
