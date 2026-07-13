using Exam.WebApp.Services;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Dashboard : AdminPageBase
{
    private DashboardSummaryDto? summary;

    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () => summary = await Api.GetDashboardSummaryAsync(), "Không tải được tổng quan");

    private static string ActionLabel(string action) => action switch
    {
        "User.RoleChanged" => "Đổi role người dùng",
        "Role.PermissionsChanged" => "Đổi quyền theo Role",
        "Category.Create" => "Tạo môn học",
        "Question.Create" => "Thêm câu hỏi",
        "Exam.Publish" => "Xuất bản đề thi",
        _ => action
    };
}
