using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Categories : ComponentBase
{
    private IReadOnlyCollection<CategoryDto>? categories;
    private bool isLoading;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            categories = await Api.GetCategoriesAsync();
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Không tải được danh sách: {ex.Message}", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<CategoryFormDialog> { { x => x.Model, new CategoryRequest("", "") } };
        var dialog = await DialogService.ShowAsync<CategoryFormDialog>("Thêm môn học", parameters);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: CategoryRequest data })
        {
            try
            {
                await Api.CreateCategoryAsync(data);
                Snackbar.Add("Đã thêm môn học.", Severity.Success);
                await LoadAsync();
            }
            catch (ExamApiException ex)
            {
                Snackbar.Add($"Tạo thất bại: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task OpenEditDialog(CategoryDto category)
    {
        var parameters = new DialogParameters<CategoryFormDialog>
        {
            { x => x.Model, new CategoryRequest(category.Name, category.UrlPath) }
        };
        var dialog = await DialogService.ShowAsync<CategoryFormDialog>("Sửa môn học", parameters);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: CategoryRequest data })
        {
            try
            {
                await Api.UpdateCategoryAsync(category.Id, data);
                Snackbar.Add("Đã cập nhật.", Severity.Success);
                await LoadAsync();
            }
            catch (ExamApiException ex)
            {
                Snackbar.Add($"Cập nhật thất bại: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task DeleteAsync(CategoryDto category)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Xác nhận xoá", $"Xoá môn học '{category.Name}'?", yesText: "Xoá", cancelText: "Huỷ");

        if (confirmed != true)
            return;

        try
        {
            await Api.DeleteCategoryAsync(category.Id);
            Snackbar.Add("Đã xoá.", Severity.Success);
            await LoadAsync();
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Xoá thất bại (có thể còn Question/Exam đang dùng): {ex.Message}", Severity.Error);
        }
    }
}
