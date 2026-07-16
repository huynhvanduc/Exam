using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;

namespace Exam.Infrastructure.IntegrationTest.Fakes;

public class FakeQuestionRepository : InMemoryRepositoryBase<Question>, IQuestionRepository
{
    public Task<IReadOnlyCollection<Question>> GetByCategoryAsync(string categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Question>>(Items.Where(x => x.CategoryId == categoryId).ToList());

    public Task<IReadOnlyCollection<Question>> GetByCategoryAsync(string categoryId, int skip, int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Question>>(Items.Where(x => x.CategoryId == categoryId).Skip(skip).Take(take).ToList());

    public Task<long> CountByCategoryAsync(string categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult((long)Items.Count(x => x.CategoryId == categoryId));

    public Task<IReadOnlyCollection<Question>> GetByCategoryAsync(string categoryId, int skip, int take, Level? level,
        QuestionType? questionType, string? keyword, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Question>>(Filter(categoryId, level, questionType, keyword).Skip(skip).Take(take).ToList());

    public Task<long> CountByCategoryAsync(string categoryId, Level? level, QuestionType? questionType, string? keyword,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((long)Filter(categoryId, level, questionType, keyword).Count());

    public Task<IReadOnlyCollection<Question>> GetByCategoryLevelTypeAsync(string categoryId, Level level, QuestionType questionType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Question>>(Items
            .Where(x => x.CategoryId == categoryId && x.Level == level && x.QuestionType == questionType).ToList());

    public Task<IReadOnlyCollection<Question>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idSet = ids.ToHashSet();
        return Task.FromResult<IReadOnlyCollection<Question>>(Items.Where(x => idSet.Contains(x.Id)).ToList());
    }

    public Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(x => x.CategoryId == categoryId));

    public Task<long> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult((long)Items.Count);

    private IEnumerable<Question> Filter(string categoryId, Level? level, QuestionType? questionType, string? keyword) => Items
        .Where(x => x.CategoryId == categoryId)
        .Where(x => !level.HasValue || x.Level == level.Value)
        .Where(x => !questionType.HasValue || x.QuestionType == questionType.Value)
        .Where(x => string.IsNullOrWhiteSpace(keyword) || x.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase));
}
