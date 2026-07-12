using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using MediatR;
using MongoDB.Bson;

namespace Exam.Application.QuestionAggregate.Commands.UpdateQuestion;

public class UpdateQuestionCommandHandler : IRequestHandler<UpdateQuestionCommand, QuestionDto>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ICategoryRepository _categoryRepository;

    public UpdateQuestionCommandHandler(IQuestionRepository questionRepository, ICategoryRepository categoryRepository)
    {
        _questionRepository = questionRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<QuestionDto> Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Question), request.Id);

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.CategoryId);

        var answers = request.Answers
            .Select(a => new Answer(ObjectId.GenerateNewId().ToString(), a.Content, a.IsCorrect))
            .ToList();

        question.Update(request.Content, request.QuestionType, request.Level, category.Id, category.Name,
            answers, request.Explain, request.Points);

        await _questionRepository.UpdateAsync(question, cancellationToken);

        return QuestionMapper.ToDto(question);
    }
}
