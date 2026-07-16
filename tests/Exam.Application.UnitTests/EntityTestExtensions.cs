using Exam.Domain.SeedWork;

namespace Exam.Application.UnitTests;

// Trong thực tế, Id của aggregate (vd Category) do MongoDB driver gán khi Insert - các test dựng aggregate
// hoàn toàn trong bộ nhớ (không qua Mongo) nên Id luôn null. Một số handler (CreateQuestion, UpdateQuestion,
// MoveQuestions, ImportQuestions...) lại lấy category.Id để gán làm CategoryId của Question, và Question yêu
// cầu CategoryId khác rỗng - cần gán Id giả lập qua reflection để mô phỏng "category đã tồn tại trong DB".
public static class EntityTestExtensions
{
    public static T WithId<T>(this T entity, string id) where T : Entity
    {
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);
        return entity;
    }
}
