using Exam.Domain.AggregateModels.QuestionAggregate;
using MediatR;

namespace Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategory;

public class GetQuestionsByCategoryQueryHandler : IRequestHandler<GetQuestionsByCategoryQuery, IReadOnlyCollection<QuestionDto>>
{
    private readonly IQuestionRepository _questionRepository;

    public GetQuestionsByCategoryQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<IReadOnlyCollection<QuestionDto>> Handle(GetQuestionsByCategoryQuery request, CancellationToken cancellationToken)
    {
        var questions = await _questionRepository.GetByCategoryAsync(request.CategoryId, cancellationToken);

        return questions.Select(QuestionMapper.ToDto).ToList();
    }
}
