using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamResultAggregate;

public interface IExamResultRepository : IRepositoryBase<ExamResult>
{
    Task<ExamResult> GetInProgressAttemptAsync(string userId, string examId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ExamResult>> GetByUserIdAsync(string userId, int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
