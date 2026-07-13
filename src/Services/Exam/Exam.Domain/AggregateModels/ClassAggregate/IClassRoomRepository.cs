using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ClassAggregate;

public interface IClassRoomRepository : IRepositoryBase<ClassRoom>
{
    Task<ClassRoom> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ClassRoom>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ClassRoom>> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ClassRoom>> GetByMemberAsync(string userId, CancellationToken cancellationToken = default);
}
