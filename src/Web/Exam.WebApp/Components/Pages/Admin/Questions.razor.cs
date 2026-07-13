using Exam.WebApp.Services;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Questions : AdminPageBase
{
    private IReadOnlyCollection<CategoryDto>? categories;
    private MudTable<QuestionDto>? table;
    private string? selectedCategoryId;
    private string? keyword;
    private Level? levelFilter;
    private QuestionType? questionTypeFilter;

    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () => categories = await Api.GetCategoriesAsync(), "Không tải được danh sách môn học");

    private async Task OnCategoryChangedAsync(string categoryId)
    {
        selectedCategoryId = categoryId;
        if (table != null)
            await table.ReloadServerData();
    }

    private async Task OnKeywordChangedAsync(string value)
    {
        keyword = value;
        if (table != null)
            await table.ReloadServerData();
    }

    private async Task OnLevelChangedAsync(Level? value)
    {
        levelFilter = value;
        if (table != null)
            await table.ReloadServerData();
    }

    private async Task OnQuestionTypeChangedAsync(QuestionType? value)
    {
        questionTypeFilter = value;
        if (table != null)
            await table.ReloadServerData();
    }

    private Task<TableData<QuestionDto>> LoadServerData(TableState state, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(selectedCategoryId))
            return Task.FromResult(new TableData<QuestionDto> { Items = [], TotalItems = 0 });

        return LoadTableDataAsync(async () =>
        {
            var result = await Api.GetQuestionsByCategoryPagedAsync(selectedCategoryId, state.Page + 1, state.PageSize,
                levelFilter, questionTypeFilter, keyword, cancellationToken);
            return new TableData<QuestionDto> { Items = result.Items, TotalItems = (int)result.TotalCount };
        }, "Không tải được danh sách câu hỏi");
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<QuestionFormDialog> { { x => x.CategoryId, selectedCategoryId! } };
        var data = await ShowFormDialogAsync<QuestionFormDialog, QuestionRequest>("Thêm câu hỏi", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        if (data == null)
            return;

        await ExecuteAsync(() => Api.CreateQuestionAsync(data), "Tạo thất bại", "Đã thêm câu hỏi.");
        if (table != null)
            await table.ReloadServerData();
    }

    private async Task OpenEditDialog(QuestionDto question)
    {
        var parameters = new DialogParameters<QuestionFormDialog>
        {
            { x => x.CategoryId, question.CategoryId },
            { x => x.Model, question }
        };
        var data = await ShowFormDialogAsync<QuestionFormDialog, QuestionRequest>("Sửa câu hỏi", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        if (data == null)
            return;

        await ExecuteAsync(() => Api.UpdateQuestionAsync(question.Id, data), "Cập nhật thất bại", "Đã cập nhật.");
        if (table != null)
            await table.ReloadServerData();
    }

    private Task DeleteAsync(QuestionDto question) => ConfirmAndExecuteAsync(
        "Xác nhận xoá", $"Xoá câu hỏi '{Truncate(question.Content)}'?",
        async () =>
        {
            await Api.DeleteQuestionAsync(question.Id);
            if (table != null)
                await table.ReloadServerData();
        },
        "Xoá thất bại (có thể còn đề thi đang dùng)", "Đã xoá.");

    private static string Truncate(string content) => content.Length <= 80 ? content : content[..80] + "…";

    private static string QuestionTypeLabel(QuestionType type) => type switch
    {
        QuestionType.SingleSelection => "Một đáp án đúng",
        QuestionType.MultipleSelection => "Nhiều đáp án đúng",
        _ => type.ToString()
    };

}
