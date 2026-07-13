using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.UserAggregate;

public interface IUserRepository : IRepositoryBase<User>
{
    Task<User> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<User>> GetByExternalIdsAsync(IEnumerable<string> externalIds, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<User>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountAsync(CancellationToken cancellationToken = default);
}
