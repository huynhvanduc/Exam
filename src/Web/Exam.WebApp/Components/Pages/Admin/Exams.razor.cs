using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Exams : ComponentBase
{
    private IReadOnlyCollection<CategoryDto>? categories;
    private IReadOnlyCollection<ExamDto> exams = [];
    private string? selectedCategoryId;
    private bool isLoading;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            categories = await Api.GetCategoriesAsync();
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Không tải được danh sách môn học: {ex.Message}", Severity.Error);
        }
    }

    private void NavigateToDetail(string examId) => Navigation.NavigateTo($"/admin/exams/{examId}");

    private async Task OnCategoryChangedAsync(string categoryId)
    {
        selectedCategoryId = categoryId;
        await LoadExamsAsync();
    }

    private async Task LoadExamsAsync()
    {
        if (string.IsNullOrEmpty(selectedCategoryId))
            return;

        isLoading = true;
        try
        {
            exams = await Api.GetExamsByCategoryAsync(selectedCategoryId);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Không tải được danh sách đề thi: {ex.Message}", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<ExamFormDialog> { { x => x.CategoryId, selectedCategoryId! } };
        var dialog = await DialogService.ShowAsync<ExamFormDialog>("Thêm đề thi", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: ExamRequest data })
        {
            try
            {
                await Api.CreateExamAsync(data);
                Snackbar.Add("Đã thêm đề thi.", Severity.Success);
                await LoadExamsAsync();
            }
            catch (ExamApiException ex)
            {
                Snackbar.Add($"Tạo thất bại: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task OpenEditDialog(ExamDto exam)
    {
        var parameters = new DialogParameters<ExamFormDialog>
        {
            { x => x.CategoryId, exam.CategoryId },
            { x => x.Model, exam }
        };
        var dialog = await DialogService.ShowAsync<ExamFormDialog>("Sửa đề thi", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: ExamRequest data })
        {
            try
            {
                await Api.UpdateExamAsync(exam.Id, data);
                Snackbar.Add("Đã cập nhật.", Severity.Success);
                await LoadExamsAsync();
            }
            catch (ExamApiException ex)
            {
                Snackbar.Add($"Cập nhật thất bại: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task DeleteAsync(ExamDto exam)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Xác nhận xoá", $"Xoá đề thi '{exam.Name}'?", yesText: "Xoá", cancelText: "Huỷ");

        if (confirmed != true)
            return;

        try
        {
            await Api.DeleteExamAsync(exam.Id);
            Snackbar.Add("Đã xoá.", Severity.Success);
            await LoadExamsAsync();
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Xoá thất bại: {ex.Message}", Severity.Error);
        }
    }

    private static string LevelLabel(Level level) => level switch
    {
        Level.Easy => "Dễ",
        Level.Medium => "Trung bình",
        Level.Difficult => "Khó",
        _ => level.ToString()
    };

    private static string StatusLabel(ExamStatus status) => status switch
    {
        ExamStatus.Draft => "Nháp",
        ExamStatus.Published => "Đã xuất bản",
        ExamStatus.Archived => "Lưu trữ",
        _ => status.ToString()
    };

    private static Color StatusColor(ExamStatus status) => status switch
    {
        ExamStatus.Draft => Color.Default,
        ExamStatus.Published => Color.Success,
        ExamStatus.Archived => Color.Dark,
        _ => Color.Default
    };
}
