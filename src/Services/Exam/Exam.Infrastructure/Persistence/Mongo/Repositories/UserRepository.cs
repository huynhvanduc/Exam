using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class UserRepository : MongoRepositoryBase<User>, IUserRepository
{
    public UserRepository(MongoDbContext context, ILogger<UserRepository> logger, IMediator mediator)
        : base(context, "users", logger, mediator)
    {
    }

    public Task<User> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting User by ExternalId {ExternalId}.", externalId);
        return Collection.Find(x => x.ExternalId == externalId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<User>> GetByExternalIdsAsync(IEnumerable<string> externalIds, CancellationToken cancellationToken = default)
    {
        var idList = externalIds?.ToList() ?? new List<string>();
        Logger.LogDebug("Getting Users by ExternalIds {ExternalIds}.", idList);
        return await Collection.Find(x => idList.Contains(x.ExternalId)).ToListAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return Collection.Find(FilterDefinition<User>.Empty).AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting all Users.");
        return await Collection.Find(FilterDefinition<User>.Empty).ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<User>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        FindPagedAsync(FilterDefinition<User>.Empty, Builders<User>.Sort.Ascending(x => x.Id), skip, take, cancellationToken);

    public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
        CountFilteredAsync(FilterDefinition<User>.Empty, cancellationToken);
}
