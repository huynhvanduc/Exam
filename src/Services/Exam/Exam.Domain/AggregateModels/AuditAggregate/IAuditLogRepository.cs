using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.AuditAggregate;

public interface IAuditLogRepository : IRepositoryBase<AuditLogEntry>
{
    Task<IReadOnlyCollection<AuditLogEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
}
