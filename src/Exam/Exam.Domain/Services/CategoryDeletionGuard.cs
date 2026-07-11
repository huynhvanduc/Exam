using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.Services;

public class CategoryDeletionGuard
{
    private readonly IExamRepository _examRepository;
    private readonly IQuestionRepository _questionRepository;

    public CategoryDeletionGuard(IExamRepository examRepository, IQuestionRepository questionRepository)
    {
        _examRepository = examRepository;
        _questionRepository = questionRepository;
    }

    public async Task EnsureCanDeleteAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        if (await _examRepository.ExistsByCategoryIdAsync(categoryId, cancellationToken))
            throw new ExamDomainException("Cannot delete category: it still has exams assigned to it.");

        if (await _questionRepository.ExistsByCategoryIdAsync(categoryId, cancellationToken))
            throw new ExamDomainException("Cannot delete category: it still has questions assigned to it.");
    }
}
