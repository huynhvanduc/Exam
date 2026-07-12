using Exam.Domain.AggregateModels.AuditAggregate;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class AuditLogRepository : MongoRepositoryBase<AuditLogEntry>, IAuditLogRepository
{
    public AuditLogRepository(MongoDbContext context, ILogger<AuditLogRepository> logger, IMediator mediator)
        : base(context, "auditLog", logger, mediator)
    {
    }

    public async Task<IReadOnlyCollection<AuditLogEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting {Limit} most recent AuditLogEntries.", limit);
        return await Collection.Find(FilterDefinition<AuditLogEntry>.Empty)
            .SortByDescending(x => x.Timestamp)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }
}
