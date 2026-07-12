using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Contracts;
using Exam.Domain.Exceptions;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Domain.Services;

public class ExamQuestionPoolService
{
    private readonly IQuestionRepository _questionRepository;

    public ExamQuestionPoolService(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<IReadOnlyCollection<string>> DrawQuestionIdsAsync(ExamEntity exam, CancellationToken cancellationToken = default)
    {
        if (exam.QuestionSelectionMode != QuestionSelectionMode.Pool)
            throw new ExamDomainException("Exam is not configured to use a question pool.");

        var candidates = await _questionRepository.GetByCategoryAsync(exam.PoolCategoryId, cancellationToken);

        if (candidates.Count < exam.PoolQuestionCount)
            throw new ExamDomainException(
                $"Not enough questions in category '{exam.PoolCategoryId}' to draw {exam.PoolQuestionCount} questions (found {candidates.Count}).");

        var pool = candidates.Select(q => q.Id).ToArray();
        Random.Shared.Shuffle(pool);

        return pool.Take(exam.PoolQuestionCount).ToList();
    }
}
