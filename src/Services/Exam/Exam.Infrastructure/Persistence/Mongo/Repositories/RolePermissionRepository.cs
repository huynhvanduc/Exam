using Exam.Contracts;
using Exam.Domain.AggregateModels.RoleAggregate;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class RolePermissionRepository : MongoRepositoryBase<RolePermissionSet>, IRolePermissionRepository
{
    public RolePermissionRepository(MongoDbContext context, ILogger<RolePermissionRepository> logger, IMediator mediator)
        : base(context, "rolePermissions", logger, mediator)
    {
    }

    public Task<RolePermissionSet> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting RolePermissionSet for Role {Role}.", role);
        return Collection.Find(x => x.Role == role).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RolePermissionSet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting all RolePermissionSets.");
        return await Collection.Find(FilterDefinition<RolePermissionSet>.Empty).ToListAsync(cancellationToken);
    }
}
