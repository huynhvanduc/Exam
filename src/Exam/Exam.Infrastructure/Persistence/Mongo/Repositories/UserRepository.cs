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

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return Collection.Find(FilterDefinition<User>.Empty).AnyAsync(cancellationToken);
    }
}
