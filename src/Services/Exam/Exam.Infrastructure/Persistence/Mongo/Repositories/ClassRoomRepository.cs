using Exam.Domain.AggregateModels.ClassAggregate;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class ClassRoomRepository : MongoRepositoryBase<ClassRoom>, IClassRoomRepository
{
    public ClassRoomRepository(MongoDbContext context, ILogger<ClassRoomRepository> logger, IMediator mediator)
        : base(context, "classes", logger, mediator)
    {
    }

    public Task<ClassRoom> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting ClassRoom by JoinCode {JoinCode}.", joinCode);
        return Collection.Find(x => x.JoinCode == joinCode).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClassRoom>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting all ClassRooms.");
        return await Collection.Find(FilterDefinition<ClassRoom>.Empty).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClassRoom>> GetByMemberAsync(string userId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting ClassRooms by member {UserId}.", userId);
        return await Collection.Find(x => x.MemberUserIds.Contains(userId)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClassRoom>> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting ClassRooms by owner {OwnerUserId}.", ownerUserId);
        return await Collection.Find(x => x.OwnerUserId == ownerUserId).ToListAsync(cancellationToken);
    }
}
