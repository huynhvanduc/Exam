using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Users : ComponentBase
{
    private IReadOnlyCollection<UserDto>? users;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            users = await Api.GetUsersAsync();
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Không tải được danh sách người dùng: {ex.Message}", Severity.Error);
        }
    }

    private async Task ChangeRoleAsync(UserDto user, UserRole role)
    {
        if (role == user.Role)
            return;

        try
        {
            await Api.PromoteUserRoleAsync(user.ExternalId, new PromoteUserRoleRequest(role));
            Snackbar.Add($"Đã đổi vai trò của {user.FirstName} {user.LastName} thành {role}.", Severity.Success);
            await LoadAsync();
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Đổi vai trò thất bại: {ex.Message}", Severity.Error);
        }
    }
}
