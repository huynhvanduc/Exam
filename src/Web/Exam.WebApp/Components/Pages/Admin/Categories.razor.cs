using Exam.WebApp.Services;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Categories : AdminPageBase
{
    private IReadOnlyCollection<CategoryDto>? categories;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync() =>
        await ExecuteAsync(async () => categories = await Api.GetCategoriesAsync(), "Không tải được danh sách");

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<CategoryFormDialog> { { x => x.Model, new CategoryRequest("", "") } };
        var data = await ShowFormDialogAsync<CategoryFormDialog, CategoryRequest>("Thêm môn học", parameters);
        if (data == null)
            return;

        await ExecuteAsync(() => Api.CreateCategoryAsync(data), "Tạo thất bại", "Đã thêm môn học.");
        await LoadAsync();
    }

    private async Task OpenEditDialog(CategoryDto category)
    {
        var parameters = new DialogParameters<CategoryFormDialog>
        {
            { x => x.Model, new CategoryRequest(category.Name, category.UrlPath) }
        };
        var data = await ShowFormDialogAsync<CategoryFormDialog, CategoryRequest>("Sửa môn học", parameters);
        if (data == null)
            return;

        await ExecuteAsync(() => Api.UpdateCategoryAsync(category.Id, data), "Cập nhật thất bại", "Đã cập nhật.");
        await LoadAsync();
    }

    private Task DeleteAsync(CategoryDto category) => ConfirmAndExecuteAsync(
        "Xác nhận xoá", $"Xoá môn học '{category.Name}'?",
        async () =>
        {
            await Api.DeleteCategoryAsync(category.Id);
            await LoadAsync();
        },
        "Xoá thất bại (có thể còn Question/Exam đang dùng)", "Đã xoá.");
}
