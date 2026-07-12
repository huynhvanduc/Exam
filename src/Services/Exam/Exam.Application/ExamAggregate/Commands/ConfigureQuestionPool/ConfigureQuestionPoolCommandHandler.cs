using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ConfigureQuestionPool;

public class ConfigureQuestionPoolCommandHandler : IRequestHandler<ConfigureQuestionPoolCommand, ExamDto>
{
    private readonly IExamRepository _examRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IQuestionRepository _questionRepository;

    public ConfigureQuestionPoolCommandHandler(IExamRepository examRepository, ICategoryRepository categoryRepository,
        IQuestionRepository questionRepository)
    {
        _examRepository = examRepository;
        _categoryRepository = categoryRepository;
        _questionRepository = questionRepository;
    }

    public async Task<ExamDto> Handle(ConfigureQuestionPoolCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        var category = await _categoryRepository.GetByIdAsync(request.PoolCategoryId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.PoolCategoryId);

        var availableQuestions = await _questionRepository.GetByCategoryAsync(category.Id, cancellationToken);

        if (availableQuestions.Count < request.PoolQuestionCount)
            throw new ExamDomainException(
                $"Not enough questions in category '{category.Name}' to draw {request.PoolQuestionCount} questions (found {availableQuestions.Count}).");

        exam.ConfigureQuestionPool(category.Id, request.PoolQuestionCount);

        await _examRepository.UpdateAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
