using Exam.Domain.SeedWork;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public abstract class MongoRepositoryBase<T> : IRepositoryBase<T> where T : Entity, IAggregateRoot
{
    protected IMongoCollection<T> Collection { get; }

    protected ILogger Logger { get; }

    private readonly IMediator _mediator;

    protected MongoRepositoryBase(MongoDbContext context, string collectionName, ILogger logger, IMediator mediator)
    {
        Collection = context.GetCollection<T>(collectionName);
        Logger = logger;
        _mediator = mediator;
    }

    public virtual Task<T> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting {Type} {Id}.", typeof(T).Name, id);
        return Collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task InsertAsync(T obj, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Inserting {Type} {Id}.", typeof(T).Name, obj.Id);
        await Collection.InsertOneAsync(obj, options: (InsertOneOptions?)null, cancellationToken);
        await DispatchDomainEventsAsync(obj, cancellationToken);
    }

    public virtual async Task UpdateAsync(T obj, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Updating {Type} {Id}.", typeof(T).Name, obj.Id);
        await Collection.ReplaceOneAsync(x => x.Id == obj.Id, obj, options: (ReplaceOptions?)null, cancellationToken);
        await DispatchDomainEventsAsync(obj, cancellationToken);
    }

    public virtual Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Deleting {Type} {Id}.", typeof(T).Name, id);
        return Collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
    }

    protected async Task<IReadOnlyCollection<T>> FindPagedAsync(FilterDefinition<T> filter, SortDefinition<T> sort, int skip, int take, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Getting {Type}, skip {Skip}, take {Take}.", typeof(T).Name, skip, take);
        return await Collection.Find(filter).Sort(sort).Skip(skip).Limit(take).ToListAsync(cancellationToken);
    }

    protected Task<long> CountFilteredAsync(FilterDefinition<T> filter, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Counting {Type}.", typeof(T).Name);
        return Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(T obj, CancellationToken cancellationToken)
    {
        var domainEvents = obj.DomainEvents;
        if (domainEvents == null || domainEvents.Count == 0)
            return;

        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, cancellationToken);

        obj.ClearDomainEvents();
    }
}
