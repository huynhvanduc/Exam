using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Domain.UnitTests.Fakes;

// Fake tối giản cho IExamRepository - chỉ cài đủ method mà các Domain Service dưới test thực sự gọi tới,
// phần còn lại ném NotImplementedException để lộ ngay nếu code test vô tình phụ thuộc method chưa cấu hình.
public class FakeExamRepository : IExamRepository
{
    public Dictionary<string, ExamEntity> ExamsById { get; } = new();
    public bool ExistsByCategoryIdResult { get; set; }

    public Task<ExamEntity> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(ExamsById.TryGetValue(id, out var exam) ? exam : null!);

    public Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ExistsByCategoryIdResult);

    public Task InsertAsync(ExamEntity obj, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task UpdateAsync(ExamEntity obj, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyCollection<ExamEntity>> GetByCategoryAsync(string categoryId, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<long> CountByCategoryAsync(string categoryId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyCollection<ExamEntity>> GetAvailableForUserAsync(DateTime at, IReadOnlyCollection<string> classIds, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<long> CountAvailableForUserAsync(DateTime at, IReadOnlyCollection<string> classIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<long> CountAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<long> CountByStatusAsync(ExamStatus status, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
