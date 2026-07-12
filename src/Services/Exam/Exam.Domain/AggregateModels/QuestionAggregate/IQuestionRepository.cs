using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.QuestionAggregate;

public interface IQuestionRepository : IRepositoryBase<Question>
{
    Task<IReadOnlyCollection<Question>> GetByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Question>> GetByCategoryAsync(string categoryId, int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Question>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default);
}
