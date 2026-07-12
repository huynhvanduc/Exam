using Exam.WebApp.Services;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Permissions : AdminPageBase
{
    private record PermissionRow(string Key, string Label, string GroupName);

    private static readonly IReadOnlyList<PermissionRow> AllRows =
    [
        new(Exam.Contracts.Permissions.Category.Create, "Tạo môn học", "Môn học"),
        new(Exam.Contracts.Permissions.Category.Update, "Sửa môn học", "Môn học"),
        new(Exam.Contracts.Permissions.Category.Delete, "Xoá môn học", "Môn học"),

        new(Exam.Contracts.Permissions.Question.View, "Xem ngân hàng câu hỏi", "Câu hỏi"),
        new(Exam.Contracts.Permissions.Question.Create, "Tạo câu hỏi", "Câu hỏi"),
        new(Exam.Contracts.Permissions.Question.Update, "Sửa câu hỏi", "Câu hỏi"),
        new(Exam.Contracts.Permissions.Question.Delete, "Xoá câu hỏi", "Câu hỏi"),

        new(Exam.Contracts.Permissions.Exam.Create, "Tạo đề thi", "Đề thi"),
        new(Exam.Contracts.Permissions.Exam.Update, "Sửa đề thi", "Đề thi"),
        new(Exam.Contracts.Permissions.Exam.Delete, "Xoá đề thi", "Đề thi"),
        new(Exam.Contracts.Permissions.Exam.ManageQuestions, "Quản lý câu hỏi trong đề", "Đề thi"),
        new(Exam.Contracts.Permissions.Exam.ManagePool, "Cấu hình pool câu hỏi", "Đề thi"),
        new(Exam.Contracts.Permissions.Exam.ManageAvailability, "Cấu hình lịch phát hành", "Đề thi"),
        new(Exam.Contracts.Permissions.Exam.ManageNegativeMarking, "Cấu hình trừ điểm", "Đề thi"),
        new(Exam.Contracts.Permissions.Exam.Publish, "Xuất bản đề thi", "Đề thi"),
        new(Exam.Contracts.Permissions.Exam.Unpublish, "Chuyển đề thi về Nháp", "Đề thi"),
        new(Exam.Contracts.Permissions.Exam.Archive, "Lưu trữ đề thi", "Đề thi"),

        new(Exam.Contracts.Permissions.User.PromoteRole, "Đổi vai trò người dùng", "Người dùng"),
    ];

    private IReadOnlyList<PermissionRow>? rows;
    private Dictionary<string, bool> studentChecks = new();
    private Dictionary<string, bool> instructorChecks = new();

    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () =>
        {
            var rolePermissions = await Api.GetRolePermissionsAsync();
            var studentPermissions = rolePermissions.FirstOrDefault(r => r.Role == UserRole.Student)?.Permissions ?? [];
            var instructorPermissions = rolePermissions.FirstOrDefault(r => r.Role == UserRole.Instructor)?.Permissions ?? [];

            studentChecks = AllRows.ToDictionary(r => r.Key, r => studentPermissions.Contains(r.Key));
            instructorChecks = AllRows.ToDictionary(r => r.Key, r => instructorPermissions.Contains(r.Key));
            rows = AllRows;
        }, "Không tải được danh sách quyền");

    private Task SaveAsync()
    {
        var studentSelected = studentChecks.Where(x => x.Value).Select(x => x.Key).ToList();
        var instructorSelected = instructorChecks.Where(x => x.Value).Select(x => x.Key).ToList();

        return ExecuteAsync(async () =>
        {
            await Api.UpdateRolePermissionsAsync(UserRole.Student, new UpdateRolePermissionsRequest(studentSelected));
            await Api.UpdateRolePermissionsAsync(UserRole.Instructor, new UpdateRolePermissionsRequest(instructorSelected));
        }, "Lưu thất bại", "Đã lưu phân quyền.");
    }
}
