using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using MediatR;
using MongoDB.Bson;

namespace Exam.Application.QuestionAggregate.Commands.CreateQuestion;

public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, QuestionDto>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreateQuestionCommandHandler(IQuestionRepository questionRepository, ICategoryRepository categoryRepository)
    {
        _questionRepository = questionRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<QuestionDto> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.CategoryId);

        var answers = request.Answers
            .Select(a => new Answer(ObjectId.GenerateNewId().ToString(), a.Content, a.IsCorrect))
            .ToList();

        var question = new Question(null, request.Content, request.QuestionType, request.Level, category.Id,
            answers, request.Explain, request.Points, request.OwnerUserId, category.Name);

        await _questionRepository.InsertAsync(question, cancellationToken);

        return QuestionMapper.ToDto(question);
    }
}
