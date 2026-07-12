using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class AuditLog : ComponentBase
{
    private IReadOnlyCollection<AuditLogEntryDto>? entries;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            entries = await Api.GetAuditLogAsync();
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Không tải được nhật ký: {ex.Message}", Severity.Error);
        }
    }

    private static string ActionLabel(string action) => action switch
    {
        "User.RoleChanged" => "Đổi role người dùng",
        "Role.PermissionsChanged" => "Đổi quyền theo Role",
        _ => action
    };
}
