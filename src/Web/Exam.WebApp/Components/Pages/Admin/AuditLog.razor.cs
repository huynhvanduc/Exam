using Exam.WebApp.Components.UI;
using Exam.WebApp.Services;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class AuditLog : AdminPageBase
{
    private AppTable<AuditLogEntryDto>? table;
    private string? actorFilter;
    private string? actionFilter;
    private DateTime? fromFilter;
    private DateTime? toFilter;
    private int resultCount;

    private async Task<AppTableData<AuditLogEntryDto>> LoadServerData(AppTableState state, CancellationToken cancellationToken)
    {
        var data = await LoadAppTableDataAsync(async () =>
        {
            var result = await Api.GetAuditLogAsync(state.Page + 1, state.PageSize, actorFilter, actionFilter, fromFilter, toFilter, cancellationToken);
            return new AppTableData<AuditLogEntryDto> { Items = result.Items, TotalItems = (int)result.TotalCount };
        }, "Không tải được nhật ký");
        // resultCount thuộc component cha (AuditLog) nhưng callback này chạy trong vòng đời render của
        // AppTable (component con) - AppTable tự StateHasChanged() cho chính nó sau khi ServerData xong,
        // KHÔNG tự động render lại cha, nên phải gọi StateHasChanged() ở đây để "N sự kiện" cập nhật đúng.
        resultCount = data.TotalItems;
        StateHasChanged();
        return data;
    }

    private Task ApplyFiltersAsync() => table?.ResetAndReloadAsync() ?? Task.CompletedTask;

    private Task ResetFiltersAsync()
    {
        actorFilter = null;
        actionFilter = null;
        fromFilter = null;
        toFilter = null;
        return table?.ResetAndReloadAsync() ?? Task.CompletedTask;
    }

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
