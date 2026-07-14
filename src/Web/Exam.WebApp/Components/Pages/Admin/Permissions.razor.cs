using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Permissions : AdminPageBase
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private record PermissionRow(string Key, string Label, string GroupName);

    // Nhãn hiển thị tiếng Việt cho từng quyền - CHỈ ảnh hưởng cách hiển thị, KHÔNG quyết định quyền nào
    // xuất hiện trong bảng. Danh sách quyền thực tế lấy từ Permissions.All (xem AllRows bên dưới) để
    // không bao giờ thiếu dòng khi thêm permission mới - trước đây danh sách bị hard-code riêng, thiếu
    // Exam.ManageMaxAttempts và User.ToggleActive, khiến 2 quyền này bị XOÁ MẤT mỗi khi admin bấm Lưu ở
    // trang này (SaveAsync ghi đè toàn bộ permission set bằng đúng những gì tick được trên UI).
    private static readonly Dictionary<string, (string Label, string GroupName)> Labels = new()
    {
        [Exam.Contracts.Permissions.Category.Create] = ("Tạo môn học", "Môn học"),
        [Exam.Contracts.Permissions.Category.Update] = ("Sửa môn học", "Môn học"),
        [Exam.Contracts.Permissions.Category.Delete] = ("Xoá môn học", "Môn học"),

        [Exam.Contracts.Permissions.Question.View] = ("Xem ngân hàng câu hỏi", "Câu hỏi"),
        [Exam.Contracts.Permissions.Question.Create] = ("Tạo câu hỏi", "Câu hỏi"),
        [Exam.Contracts.Permissions.Question.Update] = ("Sửa câu hỏi", "Câu hỏi"),
        [Exam.Contracts.Permissions.Question.Delete] = ("Xoá câu hỏi", "Câu hỏi"),

        [Exam.Contracts.Permissions.Exam.Create] = ("Tạo đề thi", "Đề thi"),
        [Exam.Contracts.Permissions.Exam.Update] = ("Sửa đề thi", "Đề thi"),
        [Exam.Contracts.Permissions.Exam.Delete] = ("Xoá đề thi", "Đề thi"),
        [Exam.Contracts.Permissions.Exam.ManagePool] = ("Cấu hình pool câu hỏi", "Đề thi"),
        [Exam.Contracts.Permissions.Exam.ManageAvailability] = ("Cấu hình lịch phát hành", "Đề thi"),
        [Exam.Contracts.Permissions.Exam.ManageMaxAttempts] = ("Giới hạn số lần thi lại", "Đề thi"),
        [Exam.Contracts.Permissions.Exam.Publish] = ("Xuất bản đề thi", "Đề thi"),
        [Exam.Contracts.Permissions.Exam.Unpublish] = ("Chuyển đề thi về Nháp", "Đề thi"),
        [Exam.Contracts.Permissions.Exam.Archive] = ("Lưu trữ đề thi", "Đề thi"),
        [Exam.Contracts.Permissions.Exam.ViewResults] = ("Xem kết quả thi", "Đề thi"),
        [Exam.Contracts.Permissions.Exam.ManageClassAssignment] = ("Gán đề thi vào lớp", "Đề thi"),
        [Exam.Contracts.Permissions.Exam.ForceFinishAttempt] = ("Buộc nộp bài hộ thí sinh", "Đề thi"),

        [Exam.Contracts.Permissions.Class.View] = ("Xem danh sách lớp", "Lớp học"),
        [Exam.Contracts.Permissions.Class.Create] = ("Tạo lớp học", "Lớp học"),
        [Exam.Contracts.Permissions.Class.Update] = ("Sửa lớp học", "Lớp học"),
        [Exam.Contracts.Permissions.Class.Delete] = ("Xoá lớp học", "Lớp học"),
        [Exam.Contracts.Permissions.Class.ManageMembers] = ("Quản lý thành viên lớp", "Lớp học"),

        [Exam.Contracts.Permissions.User.View] = ("Xem danh sách người dùng", "Người dùng"),
        [Exam.Contracts.Permissions.User.PromoteRole] = ("Đổi vai trò người dùng", "Người dùng"),
        [Exam.Contracts.Permissions.User.ToggleActive] = ("Khoá/mở khoá tài khoản", "Người dùng"),

        [Exam.Contracts.Permissions.Dashboard.View] = ("Xem trang Tổng quan", "Khác"),
    };

    private static readonly IReadOnlyList<PermissionRow> AllRows = Exam.Contracts.Permissions.All
        .Select(p => Labels.TryGetValue(p, out var info)
            ? new PermissionRow(p, info.Label, info.GroupName)
            : new PermissionRow(p, p, "Khác"))
        .ToList();

    private IReadOnlyList<PermissionRow>? rows;
    private Dictionary<string, bool> studentChecks = new();
    private Dictionary<string, bool> instructorChecks = new();
    private Dictionary<string, bool> savedStudentChecks = new();
    private Dictionary<string, bool> savedInstructorChecks = new();

    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () =>
        {
            var rolePermissions = await Api.GetRolePermissionsAsync();
            var studentPermissions = rolePermissions.FirstOrDefault(r => r.Role == UserRole.Student)?.Permissions ?? [];
            var instructorPermissions = rolePermissions.FirstOrDefault(r => r.Role == UserRole.Instructor)?.Permissions ?? [];

            studentChecks = AllRows.ToDictionary(r => r.Key, r => studentPermissions.Contains(r.Key));
            instructorChecks = AllRows.ToDictionary(r => r.Key, r => instructorPermissions.Contains(r.Key));
            savedStudentChecks = new Dictionary<string, bool>(studentChecks);
            savedInstructorChecks = new Dictionary<string, bool>(instructorChecks);
            rows = AllRows;
        }, "Không tải được danh sách quyền");

    private IReadOnlyList<string> DirtyKeys() =>
        AllRows.Select(r => r.Key).Where(IsRowChanged).ToList();

    private bool IsRowChanged(string key) =>
        studentChecks[key] != savedStudentChecks[key] || instructorChecks[key] != savedInstructorChecks[key];

    private async Task ToggleStudentAsync(string key)
    {
        studentChecks[key] = !studentChecks[key];
        await SyncDirtyGuardAsync();
    }

    private async Task ToggleInstructorAsync(string key)
    {
        instructorChecks[key] = !instructorChecks[key];
        await SyncDirtyGuardAsync();
    }

    private async Task SyncDirtyGuardAsync() =>
        await JS.InvokeVoidAsync("appShell.setDirtyGuard", DirtyKeys().Count > 0);

    private async Task RevertAsync()
    {
        studentChecks = new Dictionary<string, bool>(savedStudentChecks);
        instructorChecks = new Dictionary<string, bool>(savedInstructorChecks);
        Toast.Add("Đã huỷ các thay đổi chưa lưu");
        await SyncDirtyGuardAsync();
    }

    private async Task SaveAsync()
    {
        var dirty = DirtyKeys();
        if (dirty.Count == 0)
        {
            Toast.Add("Không có thay đổi nào để lưu");
            return;
        }

        var studentSelected = studentChecks.Where(x => x.Value).Select(x => x.Key).ToList();
        var instructorSelected = instructorChecks.Where(x => x.Value).Select(x => x.Key).ToList();

        await ExecuteAsync(async () =>
        {
            await Api.UpdateRolePermissionsAsync(UserRole.Student, new UpdateRolePermissionsRequest(studentSelected));
            await Api.UpdateRolePermissionsAsync(UserRole.Instructor, new UpdateRolePermissionsRequest(instructorSelected));
        }, "Lưu thất bại", $"Đã lưu {dirty.Count} thay đổi quyền.");

        savedStudentChecks = new Dictionary<string, bool>(studentChecks);
        savedInstructorChecks = new Dictionary<string, bool>(instructorChecks);
        await SyncDirtyGuardAsync();
    }
}
