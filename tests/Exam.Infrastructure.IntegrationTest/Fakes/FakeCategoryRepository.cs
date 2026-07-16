using Exam.Domain.AggregateModels.CategoryAggregate;

namespace Exam.Infrastructure.IntegrationTest.Fakes;

public class FakeCategoryRepository : InMemoryRepositoryBase<Category>, ICategoryRepository
{
    public Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Category>>(Items.ToList());

    public Task<Category> GetByUrlPathAsync(string urlPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(x => x.UrlPath == urlPath)!);
}
