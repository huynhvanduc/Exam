using Exam.Contracts;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamAggregate;

public interface IExamRepository : IRepositoryBase<Exam>
{
    Task<IReadOnlyCollection<Exam>> GetByCategoryAsync(string categoryId, int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Exam>> GetAvailableAsync(DateTime at, int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountAvailableAsync(DateTime at, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default);

    Task<long> CountAsync(CancellationToken cancellationToken = default);

    Task<long> CountByStatusAsync(ExamStatus status, CancellationToken cancellationToken = default);
}
