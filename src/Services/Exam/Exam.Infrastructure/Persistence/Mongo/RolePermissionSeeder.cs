using Exam.Contracts;
using Exam.Domain.AggregateModels.RoleAggregate;

namespace Exam.Infrastructure.Persistence.Mongo;

public static class RolePermissionSeeder
{
    public static async Task EnsureDefaultsAsync(IRolePermissionRepository repository, CancellationToken cancellationToken = default)
    {
        // Instructor mặc định có đủ quyền Category/Question/Exam như hành vi trước khi có permission-based
        // authorization (trước đây [Authorize(Roles="Instructor,Admin")]). Riêng quản lý user (xem danh sách,
        // đổi role, khoá/mở khoá) và Dashboard (số liệu toàn hệ thống) trước đây CHỈ dành cho Admin nên không
        // đưa vào mặc định của Instructor.
        var instructorDefaultPermissions = Permissions.All.Where(p => p != Permissions.User.View
            && p != Permissions.User.PromoteRole && p != Permissions.User.ToggleActive
            && p != Permissions.Dashboard.View).ToList();

        var existing = await repository.GetByRoleAsync(UserRole.Instructor, cancellationToken);
        if (existing == null)
        {
            await repository.InsertAsync(new RolePermissionSet(UserRole.Instructor, instructorDefaultPermissions), cancellationToken);
            return;
        }

        // Set đã lưu trong DB là 1 SNAPSHOT tại thời điểm seed lần đầu - mỗi khi thêm permission mới vào
        // Permissions.All (tính năng mới), Instructor role đang chạy sẽ KHÔNG tự có quyền đó, dẫn tới lỗi
        // 403 dù UI cho thao tác (đã xảy ra thực tế với Exam.ManageMaxAttempts). Tự bổ sung phần thiếu mỗi
        // lần khởi động, không đụng tới các permission admin đã chủ động tuỳ chỉnh qua trang Phân quyền.
        var missingPermissions = instructorDefaultPermissions.Except(existing.Permissions).ToList();
        if (missingPermissions.Count > 0)
        {
            existing.ReplacePermissions(existing.Permissions.Concat(missingPermissions));
            await repository.UpdateAsync(existing, cancellationToken);
        }
    }
}
