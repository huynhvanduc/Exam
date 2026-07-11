using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.UserAggregate;

public interface IUserRepository : IRepositoryBase<User>
{
    Task<User> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
}
