namespace Exam.Domain.SeedWork;

public interface IRepositoryBase<T> where T : IAggregateRoot
{
    Task<T> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task InsertAsync(T obj, CancellationToken cancellationToken = default);

    Task UpdateAsync(T obj, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
