using Exam.Domain.AggregateModels.CategoryAggregate;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class CategoryRepository : MongoRepositoryBase<Category>, ICategoryRepository
{
    private const string CollectionName = "categories";

    public CategoryRepository(MongoDbContext context, ILogger<CategoryRepository> logger, IMediator mediator)
        : base(context, CollectionName, logger, mediator)
    {
    }

    public async Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting all Categories.");
        return await Collection.Find(FilterDefinition<Category>.Empty).ToListAsync(cancellationToken);
    }

    public Task<Category> GetByUrlPathAsync(string urlPath, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting Category by UrlPath {UrlPath}.", urlPath);
        return Collection.Find(x => x.UrlPath == urlPath).FirstOrDefaultAsync(cancellationToken);
    }
}
