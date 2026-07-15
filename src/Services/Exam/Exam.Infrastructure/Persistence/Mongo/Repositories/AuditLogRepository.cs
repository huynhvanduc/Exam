using System.Text.RegularExpressions;
using Exam.Domain.AggregateModels.AuditAggregate;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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

    public Task<IReadOnlyCollection<AuditLogEntry>> GetPagedAsync(int skip, int take, string? actor, string? action,
        DateTime? from, DateTime? to, CancellationToken cancellationToken = default) =>
        FindPagedAsync(BuildFilter(actor, action, from, to), Builders<AuditLogEntry>.Sort.Descending(x => x.Timestamp), skip, take, cancellationToken);

    public Task<long> CountAsync(string? actor, string? action, DateTime? from, DateTime? to, CancellationToken cancellationToken = default) =>
        CountFilteredAsync(BuildFilter(actor, action, from, to), cancellationToken);

    private static FilterDefinition<AuditLogEntry> BuildFilter(string? actor, string? action, DateTime? from, DateTime? to)
    {
        var filter = FilterDefinition<AuditLogEntry>.Empty;

        if (!string.IsNullOrWhiteSpace(actor))
            filter &= Builders<AuditLogEntry>.Filter.Regex(x => x.ActorUserId, new BsonRegularExpression(Regex.Escape(actor), "i"));

        if (!string.IsNullOrWhiteSpace(action))
            filter &= Builders<AuditLogEntry>.Filter.Eq(x => x.Action, action);

        if (from.HasValue)
            filter &= Builders<AuditLogEntry>.Filter.Gte(x => x.Timestamp, ToUtcDayStart(from.Value));

        if (to.HasValue)
            filter &= Builders<AuditLogEntry>.Filter.Lt(x => x.Timestamp, ToUtcDayStart(to.Value).AddDays(1));

        return filter;
    }

    private static DateTime ToUtcDayStart(DateTime date) =>
        DateTime.SpecifyKind(date.Date, DateTimeKind.Local).ToUniversalTime();
}
