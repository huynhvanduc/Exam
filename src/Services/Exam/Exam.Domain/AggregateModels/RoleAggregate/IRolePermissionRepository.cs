using Exam.Contracts;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.RoleAggregate;

public interface IRolePermissionRepository : IRepositoryBase<RolePermissionSet>
{
    Task<RolePermissionSet> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RolePermissionSet>> GetAllAsync(CancellationToken cancellationToken = default);
}
