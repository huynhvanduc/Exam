using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Questions : ComponentBase
{
    private IReadOnlyCollection<CategoryDto>? categories;
    private IReadOnlyCollection<QuestionDto> questions = [];
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

    private async Task OnCategoryChangedAsync(string categoryId)
    {
        selectedCategoryId = categoryId;
        await LoadQuestionsAsync();
    }

    private async Task LoadQuestionsAsync()
    {
        if (string.IsNullOrEmpty(selectedCategoryId))
            return;

        isLoading = true;
        try
        {
            questions = await Api.GetQuestionsByCategoryAsync(selectedCategoryId);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Không tải được danh sách câu hỏi: {ex.Message}", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<QuestionFormDialog> { { x => x.CategoryId, selectedCategoryId! } };
        var dialog = await DialogService.ShowAsync<QuestionFormDialog>("Thêm câu hỏi", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: QuestionRequest data })
        {
            try
            {
                await Api.CreateQuestionAsync(data);
                Snackbar.Add("Đã thêm câu hỏi.", Severity.Success);
                await LoadQuestionsAsync();
            }
            catch (ExamApiException ex)
            {
                Snackbar.Add($"Tạo thất bại: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task OpenEditDialog(QuestionDto question)
    {
        var parameters = new DialogParameters<QuestionFormDialog>
        {
            { x => x.CategoryId, question.CategoryId },
            { x => x.Model, question }
        };
        var dialog = await DialogService.ShowAsync<QuestionFormDialog>("Sửa câu hỏi", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: QuestionRequest data })
        {
            try
            {
                await Api.UpdateQuestionAsync(question.Id, data);
                Snackbar.Add("Đã cập nhật.", Severity.Success);
                await LoadQuestionsAsync();
            }
            catch (ExamApiException ex)
            {
                Snackbar.Add($"Cập nhật thất bại: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task DeleteAsync(QuestionDto question)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Xác nhận xoá", $"Xoá câu hỏi '{Truncate(question.Content)}'?", yesText: "Xoá", cancelText: "Huỷ");

        if (confirmed != true)
            return;

        try
        {
            await Api.DeleteQuestionAsync(question.Id);
            Snackbar.Add("Đã xoá.", Severity.Success);
            await LoadQuestionsAsync();
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Xoá thất bại (có thể còn đề thi đang dùng): {ex.Message}", Severity.Error);
        }
    }

    private static string Truncate(string content) => content.Length <= 80 ? content : content[..80] + "…";

    private static string QuestionTypeLabel(QuestionType type) => type switch
    {
        QuestionType.SingleSelection => "Một đáp án đúng",
        QuestionType.MultipleSelection => "Nhiều đáp án đúng",
        _ => type.ToString()
    };

    private static string LevelLabel(Level level) => level switch
    {
        Level.Easy => "Dễ",
        Level.Medium => "Trung bình",
        Level.Difficult => "Khó",
        _ => level.ToString()
    };
}
