using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.AuditAggregate;

public interface IAuditLogRepository : IRepositoryBase<AuditLogEntry>
{
    Task<IReadOnlyCollection<AuditLogEntry>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountAsync(CancellationToken cancellationToken = default);
}
