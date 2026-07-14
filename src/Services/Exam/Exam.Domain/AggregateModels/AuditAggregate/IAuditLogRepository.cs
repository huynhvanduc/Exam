using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.AuditAggregate;

public interface IAuditLogRepository : IRepositoryBase<AuditLogEntry>
{
    Task<IReadOnlyCollection<AuditLogEntry>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AuditLogEntry>> GetPagedAsync(int skip, int take, string? actor, string? action,
        DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    Task<long> CountAsync(string? actor, string? action, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
