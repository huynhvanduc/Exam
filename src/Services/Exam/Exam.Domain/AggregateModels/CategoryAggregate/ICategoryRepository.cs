using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.CategoryAggregate;

public interface ICategoryRepository : IRepositoryBase<Category>
{
    Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Category> GetByUrlPathAsync(string urlPath, CancellationToken cancellationToken = default);
}
