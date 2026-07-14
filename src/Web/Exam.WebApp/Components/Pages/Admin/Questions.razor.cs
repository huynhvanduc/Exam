using Exam.WebApp.Components.UI;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Questions : AdminPageBase
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private IReadOnlyCollection<CategoryDto>? categories;
    private UserDto? currentUser;
    private AppTable<QuestionDto>? table;
    private string? selectedCategoryId;
    private string? keyword;
    private Level? levelFilter;
    private QuestionType? questionTypeFilter;
    private readonly HashSet<string> selectedIds = [];
    private bool showMoveModal;
    private string? moveTargetCategoryId;

    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () =>
        {
            currentUser = await Api.GetMeAsync();
            categories = await Api.GetCategoriesAsync();
        }, "Không tải được danh sách môn học");

    private async Task OnCategoryChangedAsync(string categoryId)
    {
        selectedCategoryId = categoryId;
        selectedIds.Clear();
        if (table != null)
            await table.ResetAndReloadAsync();
    }

    private async Task OnKeywordChangedAsync(string value)
    {
        keyword = value;
        if (table != null)
            await table.ResetAndReloadAsync();
    }

    private async Task OnLevelChangedAsync(Level? value)
    {
        levelFilter = value;
        if (table != null)
            await table.ResetAndReloadAsync();
    }

    private async Task OnQuestionTypeChangedAsync(QuestionType? value)
    {
        questionTypeFilter = value;
        if (table != null)
            await table.ResetAndReloadAsync();
    }

    private Task<AppTableData<QuestionDto>> LoadServerData(AppTableState state, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(selectedCategoryId))
            return Task.FromResult(new AppTableData<QuestionDto> { Items = [], TotalItems = 0 });

        return LoadAppTableDataAsync(async () =>
        {
            var result = await Api.GetQuestionsByCategoryPagedAsync(selectedCategoryId, state.Page + 1, state.PageSize,
                levelFilter, questionTypeFilter, keyword, cancellationToken);
            return new AppTableData<QuestionDto> { Items = result.Items, TotalItems = (int)result.TotalCount };
        }, "Không tải được danh sách câu hỏi");
    }

    private bool IsSelected(QuestionDto question) => selectedIds.Contains(question.Id);

    private void ToggleSelect(QuestionDto question)
    {
        if (!selectedIds.Add(question.Id))
            selectedIds.Remove(question.Id);
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new Dictionary<string, object> { ["CategoryId"] = selectedCategoryId! };
        var data = await ShowFormDialogAsync<QuestionFormDialog, QuestionRequest>("Thêm câu hỏi", parameters,
            new AppDialogOptions { Wide = true });
        if (data == null)
            return;

        await ExecuteAsync(() => Api.CreateQuestionAsync(data), "Tạo thất bại", "Đã thêm câu hỏi.");
        if (table != null)
            await table.ReloadServerData();
    }

    private async Task OpenEditDialog(QuestionDto question)
    {
        var parameters = new Dictionary<string, object>
        {
            ["CategoryId"] = question.CategoryId,
            ["Model"] = question
        };
        var data = await ShowFormDialogAsync<QuestionFormDialog, QuestionRequest>("Sửa câu hỏi", parameters,
            new AppDialogOptions { Wide = true });
        if (data == null)
            return;

        await ExecuteAsync(() => Api.UpdateQuestionAsync(question.Id, data), "Cập nhật thất bại", "Đã cập nhật.");
        if (table != null)
            await table.ReloadServerData();
    }

    private async Task OpenImportDialogAsync()
    {
        // Dialog tự lo bước Xem trước (dry-run) + Xác nhận nhập bên trong nó, chỉ đóng lại và trả về
        // kết quả cuối cùng SAU KHI admin đã xác nhận (Cancel nếu admin bỏ ngang ở bất kỳ bước nào).
        var result = await ShowFormDialogAsync<ImportQuestionsDialog, ImportQuestionsResultDto>("Nhập câu hỏi từ Excel",
            new Dictionary<string, object>());
        if (result == null)
            return;

        // Chỉ rõ câu hỏi vừa nhập vào (những) môn học nào - import đọc category theo TỪNG DÒNG trong file,
        // không theo môn học đang chọn trên trang, nên dễ nhầm nếu chỉ báo mỗi số lượng (đã xảy ra thực tế:
        // admin tưởng nhập vào môn "test" nhưng file lại ghi "Mạng máy tính" ở cột Môn học).
        var categorySummary = string.Join(", ", result.ValidRows
            .GroupBy(r => r.CategoryName)
            .Select(g => $"{g.Key} ({g.Count()})"));

        var message = result.SuccessCount > 0
            ? $"Đã nhập {result.SuccessCount}/{result.TotalRows} câu hỏi vào: {categorySummary}."
            : $"Không có câu hỏi nào được nhập ({result.TotalRows} dòng, {result.Errors.Count} lỗi).";

        Toast.Add(message, result.Errors.Count == 0 ? AppSeverity.Success : AppSeverity.Warning);

        if (table != null)
            await table.ReloadServerData();
    }

    private Task ExportAsync() => ExecuteAsync(async () =>
    {
        var bytes = await Api.ExportQuestionsAsync(selectedCategoryId!);
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("downloadFileFromBytes", $"cau-hoi-{selectedCategoryId}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", base64);
    }, "Xuất Excel thất bại");

    private Task DeleteAsync(QuestionDto question) => ConfirmAndExecuteAsync(
        "Xác nhận xoá", $"Xoá câu hỏi '{TextUtils.Truncate(question.Content, 80)}'?",
        async () =>
        {
            await Api.DeleteQuestionAsync(question.Id);
            if (table != null)
                await table.ReloadServerData();
        },
        "Xoá thất bại (có thể còn đề thi đang dùng)", "Đã xoá.");

    private Task BulkDeleteAsync()
    {
        var ids = selectedIds.ToList();
        return ConfirmAndExecuteAsync("Xác nhận xoá", $"Xoá {ids.Count} câu hỏi đã chọn?",
            async () =>
            {
                foreach (var id in ids)
                    await Api.DeleteQuestionAsync(id);
                selectedIds.Clear();
                if (table != null)
                    await table.ReloadServerData();
            }, "Xoá thất bại", $"Đã xoá {ids.Count} câu hỏi.");
    }

    private void OpenMoveModal()
    {
        moveTargetCategoryId = categories!.FirstOrDefault(c => c.Id != selectedCategoryId)?.Id;
        showMoveModal = true;
    }

    private Task ConfirmMoveAsync()
    {
        if (string.IsNullOrEmpty(moveTargetCategoryId))
            return Task.CompletedTask;

        var ids = selectedIds.ToList();
        var destName = categories!.First(c => c.Id == moveTargetCategoryId).Name;
        return ExecuteAsync(async () =>
        {
            await Api.MoveQuestionsAsync(new MoveQuestionsRequest(ids, moveTargetCategoryId));
            selectedIds.Clear();
            showMoveModal = false;
            if (table != null)
                await table.ReloadServerData();
        }, "Chuyển câu hỏi thất bại", $"Đã chuyển {ids.Count} câu hỏi sang {destName}");
    }

    private static string QuestionTypeLabel(QuestionType type) => type switch
    {
        QuestionType.SingleSelection => "Một đáp án đúng",
        QuestionType.MultipleSelection => "Nhiều đáp án đúng",
        _ => type.ToString()
    };
}
