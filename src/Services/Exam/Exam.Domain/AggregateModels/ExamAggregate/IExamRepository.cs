using Exam.Contracts;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamAggregate;

public interface IExamRepository : IRepositoryBase<Exam>
{
    Task<IReadOnlyCollection<Exam>> GetByCategoryAsync(string categoryId, int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);

    // classIds = các lớp mà user hiện tại là thành viên - đề công khai (không gán lớp nào) luôn được tính là khả dụng.
    Task<IReadOnlyCollection<Exam>> GetAvailableForUserAsync(DateTime at, IReadOnlyCollection<string> classIds, int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountAvailableForUserAsync(DateTime at, IReadOnlyCollection<string> classIds, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default);

    Task<long> CountAsync(CancellationToken cancellationToken = default);

    Task<long> CountByStatusAsync(ExamStatus status, CancellationToken cancellationToken = default);
}
