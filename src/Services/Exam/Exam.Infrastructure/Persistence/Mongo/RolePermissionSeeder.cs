using Exam.Contracts;
using Exam.Domain.AggregateModels.RoleAggregate;

namespace Exam.Infrastructure.Persistence.Mongo;

public static class RolePermissionSeeder
{
    public static async Task EnsureDefaultsAsync(IRolePermissionRepository repository, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByRoleAsync(UserRole.Instructor, cancellationToken);
        if (existing != null)
            return;

        // Instructor mặc định có đủ quyền Category/Question/Exam như hành vi trước khi có permission-based
        // authorization (trước đây [Authorize(Roles="Instructor,Admin")]). Riêng quản lý user (xem danh sách,
        // đổi role) trước đây CHỈ dành cho Admin nên không đưa vào mặc định của Instructor.
        var instructorDefaults = new RolePermissionSet(
            UserRole.Instructor,
            Permissions.All.Where(p => p != Permissions.User.View && p != Permissions.User.PromoteRole
                && p != Permissions.Dashboard.View).ToList());
        await repository.InsertAsync(instructorDefaults, cancellationToken);
    }
}
