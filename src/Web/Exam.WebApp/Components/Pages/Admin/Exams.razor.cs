using Exam.WebApp.Services;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Exams : AdminPageBase
{
    private IReadOnlyCollection<CategoryDto>? categories;
    private MudTable<ExamDto>? table;
    private string? selectedCategoryId;

    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () => categories = await Api.GetCategoriesAsync(), "Không tải được danh sách môn học");

    private void NavigateToDetail(string examId) => Navigation.NavigateTo($"/admin/exams/{examId}");

    private async Task OnCategoryChangedAsync(string categoryId)
    {
        selectedCategoryId = categoryId;
        if (table != null)
            await table.ReloadServerData();
    }

    private Task<TableData<ExamDto>> LoadServerData(TableState state, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(selectedCategoryId))
            return Task.FromResult(new TableData<ExamDto> { Items = [], TotalItems = 0 });

        return LoadTableDataAsync(async () =>
        {
            var result = await Api.GetExamsByCategoryAsync(selectedCategoryId, state.Page + 1, state.PageSize, cancellationToken);
            return new TableData<ExamDto> { Items = result.Items, TotalItems = (int)result.TotalCount };
        }, "Không tải được danh sách đề thi");
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<ExamFormDialog> { { x => x.CategoryId, selectedCategoryId! } };
        var data = await ShowFormDialogAsync<ExamFormDialog, ExamRequest>("Thêm đề thi", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        if (data == null)
            return;

        await ExecuteAsync(() => Api.CreateExamAsync(data), "Tạo thất bại", "Đã thêm đề thi.");
        if (table != null)
            await table.ReloadServerData();
    }

    private async Task OpenEditDialog(ExamDto exam)
    {
        var parameters = new DialogParameters<ExamFormDialog>
        {
            { x => x.CategoryId, exam.CategoryId },
            { x => x.Model, exam }
        };
        var data = await ShowFormDialogAsync<ExamFormDialog, ExamRequest>("Sửa đề thi", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        if (data == null)
            return;

        await ExecuteAsync(() => Api.UpdateExamAsync(exam.Id, data), "Cập nhật thất bại", "Đã cập nhật.");
        if (table != null)
            await table.ReloadServerData();
    }

    private Task DeleteAsync(ExamDto exam) => ConfirmAndExecuteAsync(
        "Xác nhận xoá", $"Xoá đề thi '{exam.Name}'?",
        async () =>
        {
            await Api.DeleteExamAsync(exam.Id);
            if (table != null)
                await table.ReloadServerData();
        },
        "Xoá thất bại", "Đã xoá.");

}
