using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamAggregate;

public interface IExamRepository : IRepositoryBase<Exam>
{
    Task<IReadOnlyCollection<Exam>> GetByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default);
}
