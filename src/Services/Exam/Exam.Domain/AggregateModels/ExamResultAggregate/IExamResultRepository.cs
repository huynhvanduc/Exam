using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamResultAggregate;

public interface IExamResultRepository : IRepositoryBase<ExamResult>
{
    Task<ExamResult> GetInProgressAttemptAsync(string userId, string examId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ExamResult>> GetByUserIdAsync(string userId, int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ExamResult>> GetByExamIdAsync(string examId, int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountByExamIdAsync(string examId, CancellationToken cancellationToken = default);
}
