using Exam.WebApp.Services;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class AuditLog : AdminPageBase
{
    private MudTable<AuditLogEntryDto>? table;

    private Task<TableData<AuditLogEntryDto>> LoadServerData(TableState state, CancellationToken cancellationToken) =>
        LoadTableDataAsync(async () =>
        {
            var result = await Api.GetAuditLogAsync(state.Page + 1, state.PageSize, cancellationToken);
            return new TableData<AuditLogEntryDto> { Items = result.Items, TotalItems = (int)result.TotalCount };
        }, "Không tải được nhật ký");

    private static string ActionLabel(string action) => action switch
    {
        "User.RoleChanged" => "Đổi role người dùng",
        "Role.PermissionsChanged" => "Đổi quyền theo Role",
        "Category.Create" => "Tạo môn học",
        "Question.Create" => "Thêm câu hỏi",
        "Exam.Publish" => "Xuất bản đề thi",
        _ => PrettifyAction(action)
    };

    // Action tự sinh bởi AuditLoggingBehavior có dạng "{Aggregate}.{Verb}" (PascalCase, vd
    // "Exam.CreateExam") - tách và chèn khoảng trắng để dễ đọc hơn thay vì hiện tên kỹ thuật thô.
    private static string PrettifyAction(string action)
    {
        var parts = action.Split('.', 2);
        return parts.Length != 2 ? action : $"{SpaceOutPascalCase(parts[0])} - {SpaceOutPascalCase(parts[1])}";
    }

    private static string SpaceOutPascalCase(string value) =>
        string.Concat(value.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}
