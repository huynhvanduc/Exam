using Exam.Domain.SeedWork;
using MongoDB.Bson;

namespace Exam.Infrastructure.IntegrationTest.Fakes;

// Base dùng chung cho mọi fake repository in-memory: mô phỏng đúng hành vi MongoDB driver thật -
// tự sinh Id (ObjectId) khi Insert nếu entity chưa có Id, UpdateAsync/DeleteAsync thao tác trực tiếp
// trên List<T> giữ trong bộ nhớ (không cần Docker/MongoDB thật để chạy integration test).
public abstract class InMemoryRepositoryBase<T> where T : Entity, IAggregateRoot
{
    public readonly List<T> Items = [];

    public Task<T> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(x => x.Id == id)!);

    public Task InsertAsync(T obj, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(obj.Id))
            SetId(obj, ObjectId.GenerateNewId().ToString());

        Items.Add(obj);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T obj, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    private static void SetId(Entity entity, string id) =>
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);
}
