using Exam.WebApp.Components.UI;
using Exam.WebApp.Services;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Categories : AdminPageBase
{
    private IReadOnlyCollection<CategoryDto>? categories;
    private AppTable<CategoryDto>? table;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync() =>
        await ExecuteAsync(async () => categories = await Api.GetCategoriesAsync(), "Không tải được danh sách");

    private Task<AppTableData<CategoryDto>> LoadTableData(AppTableState state, CancellationToken ct)
    {
        var items = categories!.Skip(state.Page * state.PageSize).Take(state.PageSize).ToList();
        return Task.FromResult(new AppTableData<CategoryDto> { Items = items, TotalItems = categories!.Count });
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new Dictionary<string, object> { ["Model"] = new CategoryRequest("", "") };
        var data = await ShowFormDialogAsync<CategoryFormDialog, CategoryRequest>("Thêm môn học", parameters);
        if (data == null)
            return;

        await ExecuteAsync(() => Api.CreateCategoryAsync(data), "Tạo thất bại", "Đã thêm môn học.");
        await LoadAsync();
        if (table != null) await table.ResetAndReloadAsync();
    }

    private async Task OpenEditDialog(CategoryDto category)
    {
        var parameters = new Dictionary<string, object>
        {
            ["Model"] = new CategoryRequest(category.Name, category.UrlPath)
        };
        var data = await ShowFormDialogAsync<CategoryFormDialog, CategoryRequest>("Sửa môn học", parameters);
        if (data == null)
            return;

        await ExecuteAsync(() => Api.UpdateCategoryAsync(category.Id, data), "Cập nhật thất bại", "Đã cập nhật.");
        await LoadAsync();
        if (table != null) await table.ReloadServerData();
    }

    private Task DeleteAsync(CategoryDto category) => ConfirmAndExecuteAsync(
        "Xác nhận xoá", $"Xoá môn học '{category.Name}'?",
        async () =>
        {
            await Api.DeleteCategoryAsync(category.Id);
            await LoadAsync();
            if (table != null) await table.ResetAndReloadAsync();
        },
        "Xoá thất bại (có thể còn Question/Exam đang dùng)", "Đã xoá.");
}
