using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.QuestionAggregate;
using MediatR;

namespace Exam.Application.QuestionAggregate.Commands.DeleteQuestion;

public class DeleteQuestionCommandHandler : IRequestHandler<DeleteQuestionCommand>
{
    private readonly IQuestionRepository _questionRepository;

    public DeleteQuestionCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Question), request.Id);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, question.OwnerUserId, nameof(Question), question.Id);

        await _questionRepository.DeleteAsync(question.Id, cancellationToken);
    }
}
