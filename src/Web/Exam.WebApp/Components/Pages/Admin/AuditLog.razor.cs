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
        _ => action
    };
}
