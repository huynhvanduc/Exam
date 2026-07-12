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

    public Task<IReadOnlyCollection<AuditLogEntry>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        FindPagedAsync(FilterDefinition<AuditLogEntry>.Empty, Builders<AuditLogEntry>.Sort.Descending(x => x.Timestamp), skip, take, cancellationToken);

    public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
        CountFilteredAsync(FilterDefinition<AuditLogEntry>.Empty, cancellationToken);
}
