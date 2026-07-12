using Exam.WebApp.Services;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Users : AdminPageBase
{
    private MudTable<UserDto>? table;

    private Task<TableData<UserDto>> LoadServerData(TableState state, CancellationToken cancellationToken) =>
        LoadTableDataAsync(async () =>
        {
            var result = await Api.GetUsersAsync(state.Page + 1, state.PageSize, cancellationToken);
            return new TableData<UserDto> { Items = result.Items, TotalItems = (int)result.TotalCount };
        }, "Không tải được danh sách người dùng");

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
}
