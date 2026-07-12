using Exam.Domain.AggregateModels.QuestionAggregate;
using MediatR;

namespace Exam.Application.QuestionAggregate.Queries.GetQuestionById;

public class GetQuestionByIdQueryHandler : IRequestHandler<GetQuestionByIdQuery, QuestionDto?>
{
    private readonly IQuestionRepository _questionRepository;

    public GetQuestionByIdQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<QuestionDto?> Handle(GetQuestionByIdQuery request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.Id, cancellationToken);

        return question == null ? null : QuestionMapper.ToDto(question);
    }
}
