using System.Security.Claims;
using Exam.WebApp.Components.UI;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Users : AdminPageBase
{
    [CascadingParameter] private Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>? AuthStateTask { get; set; }

    private AppTable<UserDto>? table;
    private string? currentUserEmail;
    private string? searchFilter;
    private UserRole? roleFilter;
    private bool? statusFilter;
    private int resultCount;

    protected override async Task OnInitializedAsync()
    {
        if (AuthStateTask != null)
        {
            var authState = await AuthStateTask;
            currentUserEmail = authState.User.FindFirst(ClaimTypes.Email)?.Value
                ?? authState.User.FindFirst("email")?.Value;
        }
    }

    private async Task<AppTableData<UserDto>> LoadServerData(AppTableState state, CancellationToken cancellationToken)
    {
        var data = await LoadAppTableDataAsync(async () =>
        {
            var result = await Api.GetUsersAsync(state.Page + 1, state.PageSize, searchFilter, roleFilter, statusFilter, cancellationToken);
            return new AppTableData<UserDto> { Items = result.Items, TotalItems = (int)result.TotalCount };
        }, "Không tải được danh sách người dùng");
        // resultCount thuộc component cha (Users) nhưng callback này chạy trong vòng đời render của
        // AppTable (component con) - AppTable tự StateHasChanged() cho chính nó sau khi ServerData xong,
        // KHÔNG tự động render lại cha, nên phải gọi StateHasChanged() ở đây để "N người dùng" cập nhật đúng.
        resultCount = data.TotalItems;
        StateHasChanged();
        return data;
    }

    private Task ApplyFiltersAsync() => table?.ResetAndReloadAsync() ?? Task.CompletedTask;

    private async Task OpenCreateDialog()
    {
        var result = await ShowFormDialogAsync<UserFormDialog, CreateUserResponse>("Thêm người dùng", []);
        if (result != null && table != null)
            await table.ResetAndReloadAsync();
    }

    private bool IsSelf(UserDto user) => user.Email == currentUserEmail;

    private async Task OpenResetPasswordDialog(UserDto user)
    {
        var parameters = new Dictionary<string, object>
        {
            ["ExternalId"] = user.ExternalId,
            ["FullName"] = $"{user.FirstName} {user.LastName}",
            ["Email"] = user.Email
        };
        await ShowFormDialogAsync<ResetPasswordDialog, ResetUserPasswordResponse>("Đặt lại mật khẩu", parameters);
    }

    private Task ChangeRoleAsync(UserDto user, UserRole role)
    {
        if (role == user.Role)
            return Task.CompletedTask;

        return ExecuteAsync(async () =>
        {
            await Api.PromoteUserRoleAsync(user.ExternalId, new PromoteUserRoleRequest(role));
            if (table != null)
                await table.ReloadServerData();
        }, "Đổi vai trò thất bại", $"Đã đổi vai trò của {user.FirstName} {user.LastName} thành {role}.");
    }

    private Task ToggleActiveAsync(UserDto user, bool isActive)
    {
        var fullName = $"{user.FirstName} {user.LastName}";
        return isActive
            ? ConfirmAndExecuteAsync("Xác nhận mở khóa", $"Mở khóa tài khoản '{fullName}'?",
                async () =>
                {
                    await Api.ToggleUserActiveAsync(user.ExternalId, new ToggleUserActiveRequest(true));
                    if (table != null)
                        await table.ReloadServerData();
                }, "Mở khóa thất bại", $"Đã mở khóa tài khoản {fullName}.", yesText: "Mở khóa")
            : ConfirmAndExecuteAsync("Xác nhận khóa", $"Khóa tài khoản '{fullName}'? Người dùng sẽ không thể sử dụng hệ thống cho tới khi được mở khóa lại.",
                async () =>
                {
                    await Api.ToggleUserActiveAsync(user.ExternalId, new ToggleUserActiveRequest(false));
                    if (table != null)
                        await table.ReloadServerData();
                }, "Khóa thất bại", $"Đã khóa tài khoản {fullName}.", yesText: "Khóa");
    }
}
