using Exam.Domain.AggregateModels.AuditAggregate;

namespace Exam.Infrastructure.IntegrationTest.Fakes;

public class FakeAuditLogRepository : InMemoryRepositoryBase<AuditLogEntry>, IAuditLogRepository
{
    public Task<IReadOnlyCollection<AuditLogEntry>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<AuditLogEntry>>(Items.OrderByDescending(x => x.Timestamp).Skip(skip).Take(take).ToList());

    public Task<long> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult((long)Items.Count);

    public Task<IReadOnlyCollection<AuditLogEntry>> GetPagedAsync(int skip, int take, string? actor, string? action,
        DateTime? from, DateTime? to, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<AuditLogEntry>>(Filter(actor, action, from, to)
            .OrderByDescending(x => x.Timestamp).Skip(skip).Take(take).ToList());

    public Task<long> CountAsync(string? actor, string? action, DateTime? from, DateTime? to, CancellationToken cancellationToken = default) =>
        Task.FromResult((long)Filter(actor, action, from, to).Count());

    private IEnumerable<AuditLogEntry> Filter(string? actor, string? action, DateTime? from, DateTime? to) => Items
        .Where(x => actor == null || x.ActorUserId == actor)
        .Where(x => action == null || x.Action == action)
        .Where(x => !from.HasValue || x.Timestamp >= from.Value)
        .Where(x => !to.HasValue || x.Timestamp <= to.Value);
}
