using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;

namespace Exam.Domain.UnitTests.Fakes;

public class FakeQuestionRepository : IQuestionRepository
{
    public Dictionary<string, Question> QuestionsById { get; } = new();
    public List<Question> CategoryLevelTypeResult { get; set; } = [];
    public bool ExistsByCategoryIdResult { get; set; }

    public Task<IReadOnlyCollection<Question>> GetByCategoryLevelTypeAsync(string categoryId, Level level,
        QuestionType questionType, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Question>>(CategoryLevelTypeResult);

    public Task<IReadOnlyCollection<Question>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Question>>(ids.Where(QuestionsById.ContainsKey).Select(id => QuestionsById[id]).ToList());

    public Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ExistsByCategoryIdResult);

    public Task<Question> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task InsertAsync(Question obj, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task UpdateAsync(Question obj, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyCollection<Question>> GetByCategoryAsync(string categoryId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyCollection<Question>> GetByCategoryAsync(string categoryId, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<long> CountByCategoryAsync(string categoryId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyCollection<Question>> GetByCategoryAsync(string categoryId, int skip, int take, Level? level, QuestionType? questionType, string? keyword, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<long> CountByCategoryAsync(string categoryId, Level? level, QuestionType? questionType, string? keyword, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<long> CountAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
