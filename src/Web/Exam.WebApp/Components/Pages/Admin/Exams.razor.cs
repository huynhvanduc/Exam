using Exam.WebApp.Components.UI;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Exams : AdminPageBase
{
    [Parameter] public string? Id { get; set; }

    private IReadOnlyCollection<CategoryDto>? categories;
    private IReadOnlyCollection<ExamDto>? exams;
    private string? selectedCategoryId;
    private ExamDto? selected;
    private UserDto? currentUser;

    // Khớp đúng OwnershipGuard.EnsureOwnerOrAdmin ở backend (Admin luôn được, Instructor chỉ được với đề
    // thi do chính mình tạo) - tính trước ở UI để ẩn/khoá các hành động chắc chắn sẽ bị 403 thay vì để
    // người dùng bấm rồi mới thấy toast lỗi.
    private bool CanManageSelected => currentUser != null && selected != null
        && (currentUser.Role == UserRole.Admin || currentUser.ExternalId == selected.OwnerUserId);

    private IReadOnlyCollection<ClassRoomDto> allClasses = [];
    private DateTime? availableFrom;
    private DateTime? availableTo;
    private bool limitMaxAttempts;
    private int maxAttempts = 1;
    private string? selectedClassId;

    private int easySingle;
    private int easyMulti;
    private int mediumSingle;
    private int mediumMulti;
    private int difficultSingle;
    private int difficultMulti;

    private int CompositionTotal => easySingle + easyMulti + mediumSingle + mediumMulti + difficultSingle + difficultMulti;

    private IReadOnlyCollection<ExamResultAdminListItemDto>? results;
    private string resultsFilter = "all";
    private ExamResultAdminListItemDto? drawerResult;
    private ExamAttemptStatusDto? drawerStatus;

    private IReadOnlyCollection<ClassRoomDto> unassignedClasses =>
        allClasses.Where(c => selected != null && !selected.AssignedClassIds.Contains(c.Id)).ToList();

    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () =>
        {
            currentUser = await Api.GetMeAsync();
            categories = await Api.GetCategoriesAsync();
        }, "Không tải được danh sách môn học");

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(Id))
        {
            selected = null;
            return;
        }

        if (selected?.Id == Id)
            return;

        await LoadSelectedAsync(Id);
    }

    private Task LoadSelectedAsync(string id) => ExecuteAsync(async () =>
    {
        selected = await Api.GetExamByIdAsync(id);
        selectedCategoryId ??= selected.CategoryId;
        availableFrom = selected.AvailableFrom;
        availableTo = selected.AvailableTo;
        limitMaxAttempts = selected.MaxAttempts.HasValue;
        maxAttempts = selected.MaxAttempts ?? 1;
        allClasses = await Api.GetClassesAsync();
        selectedClassId = null;

        easySingle = CompositionCountOf(Level.Easy, QuestionType.SingleSelection);
        easyMulti = CompositionCountOf(Level.Easy, QuestionType.MultipleSelection);
        mediumSingle = CompositionCountOf(Level.Medium, QuestionType.SingleSelection);
        mediumMulti = CompositionCountOf(Level.Medium, QuestionType.MultipleSelection);
        difficultSingle = CompositionCountOf(Level.Difficult, QuestionType.SingleSelection);
        difficultMulti = CompositionCountOf(Level.Difficult, QuestionType.MultipleSelection);

        resultsFilter = "all";
        drawerResult = null;
        drawerStatus = null;
        results = null;
        // Kết quả thi cũng bị OwnershipGuard chặn ở backend giống Sửa/Xuất bản/Lưu trữ - không gọi API
        // này khi chắc chắn sẽ bị 403 (Instructor xem đề thi của người khác), tránh toast lỗi vô nghĩa.
        if (CanManageSelected)
            await LoadResultsAsync();
    }, "Không tải được đề thi");

    private int CompositionCountOf(Level level, QuestionType questionType) =>
        selected!.Composition.FirstOrDefault(c => c.Level == level && c.QuestionType == questionType)?.Count ?? 0;

    private Task LoadResultsAsync() => ExecuteAsync(async () =>
    {
        var result = await Api.GetExamResultsByExamAsync(selected!.Id, 1, 100);
        results = result.Items;
    }, "Không tải được kết quả thi");

    private void SetResultsFilter(string filter) => resultsFilter = filter;

    private IEnumerable<ExamResultAdminListItemDto> FilteredResults() => resultsFilter switch
    {
        "doing" => results!.Where(r => !r.Finished),
        "done" => results!.Where(r => r.Finished),
        _ => results!
    };

    private async Task OpenDrawerAsync(ExamResultAdminListItemDto result)
    {
        drawerResult = result;
        drawerStatus = null;
        await ExecuteAsync(async () => drawerStatus = await Api.GetExamAttemptAdminStatusAsync(result.Id), "Không tải được tiến độ bài thi");
    }

    private bool IsAnswered(string questionId) =>
        drawerStatus?.Attempt?.SelectedAnswers.Any(a => a.QuestionId == questionId && a.SelectedAnswerIds.Count > 0) == true;

    private void CloseDrawer()
    {
        drawerResult = null;
        drawerStatus = null;
    }

    private Task ForceSubmitAsync() => ConfirmAndExecuteAsync(
        "Xác nhận buộc nộp bài", $"Buộc nộp bài của \"{drawerResult!.FullName}\"? Học viên sẽ không thể trả lời thêm.",
        async () =>
        {
            await Api.AdminForceFinishExamAsync(drawerResult.Id);
            CloseDrawer();
            await LoadResultsAsync();
        }, "Buộc nộp bài thất bại", $"Đã buộc nộp bài của {drawerResult!.FullName}", yesText: "Buộc nộp");

    private async Task OnCategoryChangedAsync(string categoryId)
    {
        selectedCategoryId = categoryId;
        await LoadListAsync();
    }

    private Task LoadListAsync()
    {
        if (string.IsNullOrEmpty(selectedCategoryId))
        {
            exams = [];
            return Task.CompletedTask;
        }

        return ExecuteAsync(async () =>
        {
            var result = await Api.GetExamsByCategoryAsync(selectedCategoryId, 1, 100);
            exams = result.Items;
        }, "Không tải được danh sách đề thi");
    }

    private void SelectExam(string id) => Navigation.NavigateTo($"/admin/exams/{id}");

    private async Task OpenCreateDialog()
    {
        var parameters = new Dictionary<string, object> { ["CategoryId"] = selectedCategoryId! };
        var data = await ShowFormDialogAsync<ExamFormDialog, ExamFormResult>("Thêm đề thi", parameters, new AppDialogOptions { Wide = true });
        if (data == null)
            return;

        string? createdId = null;
        await ExecuteAsync(async () =>
        {
            var created = await Api.CreateExamAsync(data.Exam);
            createdId = created.Id;
            await ApplyAdvancedConfigAsync(created.Id, data);
        }, "Tạo thất bại", "Đã thêm đề thi.");
        await LoadListAsync();
        if (createdId != null)
            SelectExam(createdId);
    }

    private async Task OpenEditDialog(ExamDto exam)
    {
        var parameters = new Dictionary<string, object>
        {
            ["CategoryId"] = exam.CategoryId,
            ["Model"] = exam
        };
        var data = await ShowFormDialogAsync<ExamFormDialog, ExamFormResult>("Sửa đề thi", parameters, new AppDialogOptions { Wide = true });
        if (data == null)
            return;

        await ExecuteAsync(async () =>
        {
            await Api.UpdateExamAsync(exam.Id, data.Exam);
            await ApplyAdvancedConfigAsync(exam.Id, data);
        }, "Cập nhật thất bại", "Đã cập nhật.");
        await LoadListAsync();
        if (selected?.Id == exam.Id)
            await LoadSelectedAsync(exam.Id);
    }

    // Availability/Composition/MaxAttempts vẫn là API riêng ở backend (policy quyền khác nhau) - gọi tuần
    // tự sau khi tạo/sửa đề thi thay vì bắt admin mở lại Chi tiết đề thi để cấu hình từng phần.
    private async Task ApplyAdvancedConfigAsync(string examId, ExamFormResult data)
    {
        if (data.Availability != null)
            await Api.ScheduleExamAvailabilityAsync(examId, data.Availability);
        if (data.Composition != null)
            await Api.ConfigureExamCompositionAsync(examId, data.Composition);
        if (data.MaxAttempts != null)
            await Api.ConfigureMaxAttemptsAsync(examId, data.MaxAttempts);
    }

    private Task DeleteAsync(ExamDto exam) => ConfirmAndExecuteAsync(
        "Xác nhận xoá", $"Xoá đề thi '{exam.Name}'?",
        async () =>
        {
            await Api.DeleteExamAsync(exam.Id);
            if (selected?.Id == exam.Id)
            {
                selected = null;
                Navigation.NavigateTo("/admin/exams");
            }
            await LoadListAsync();
        },
        "Xoá thất bại", "Đã xoá.");

    private string ClassName(string classId) => allClasses.FirstOrDefault(c => c.Id == classId)?.Name ?? "(Lớp không còn tồn tại)";

    private Task AssignClassAsync() => ExecuteAsync(async () =>
    {
        selected = await Api.AssignExamToClassAsync(selected!.Id, selectedClassId!);
        selectedClassId = null;
    }, "Gán lớp thất bại", "Đã gán lớp.");

    private Task UnassignClassAsync(string classId) => ExecuteAsync(async () =>
    {
        selected = await Api.UnassignExamFromClassAsync(selected!.Id, classId);
    }, "Bỏ gán thất bại", "Đã bỏ gán lớp.");

    private Task SaveCompositionAsync()
    {
        if (CompositionTotal == 0)
        {
            Toast.Add("Ma trận phải có tổng số câu lớn hơn 0.", AppSeverity.Error);
            return Task.CompletedTask;
        }

        return ExecuteAsync(async () =>
        {
            selected = await Api.ConfigureExamCompositionAsync(selected!.Id, BuildCompositionRequest());
        }, "Lưu thất bại", "Đã lưu ma trận cấu trúc.");
    }

    private ConfigureExamCompositionRequest BuildCompositionRequest()
    {
        var cells = new List<ExamCompositionCellDto>();
        void Add(Level l, QuestionType t, int n)
        {
            if (n > 0)
                cells.Add(new ExamCompositionCellDto(l, t, n));
        }
        Add(Level.Easy, QuestionType.SingleSelection, easySingle);
        Add(Level.Easy, QuestionType.MultipleSelection, easyMulti);
        Add(Level.Medium, QuestionType.SingleSelection, mediumSingle);
        Add(Level.Medium, QuestionType.MultipleSelection, mediumMulti);
        Add(Level.Difficult, QuestionType.SingleSelection, difficultSingle);
        Add(Level.Difficult, QuestionType.MultipleSelection, difficultMulti);
        return new ConfigureExamCompositionRequest(cells);
    }

    private Task SaveAvailabilityAsync() => ExecuteAsync(async () =>
    {
        selected = await Api.ScheduleExamAvailabilityAsync(selected!.Id, new ScheduleExamAvailabilityRequest(availableFrom, availableTo));
    }, "Lưu thất bại", "Đã lưu lịch phát hành.");

    private Task SaveMaxAttemptsAsync() => ExecuteAsync(async () =>
    {
        selected = await Api.ConfigureMaxAttemptsAsync(selected!.Id, new ConfigureMaxAttemptsRequest(limitMaxAttempts ? maxAttempts : null));
    }, "Lưu thất bại", "Đã lưu.");

    private Task TogglePublishAsync() => ExecuteAsync(async () =>
    {
        selected = selected!.Status == ExamStatus.Published
            ? await Api.UnpublishExamAsync(selected.Id)
            : await Api.PublishExamAsync(selected.Id);
        // Badge trạng thái ở danh sách bên trái đọc từ collection "exams", không phải "selected" -
        // phải tải lại danh sách để badge cập nhật ngay, tránh hiện sai trạng thái tới khi chọn lại đề.
        await LoadListAsync();
    }, "Thao tác thất bại", "Đã cập nhật trạng thái.");

    private Task ArchiveAsync() => ConfirmAndExecuteAsync(
        "Xác nhận lưu trữ", $"Lưu trữ đề thi '{selected!.Name}'? Không thể hoàn tác.",
        async () =>
        {
            selected = await Api.ArchiveExamAsync(selected.Id);
            await LoadListAsync();
        },
        "Lưu trữ thất bại", "Đã lưu trữ.", yesText: "Lưu trữ");

    private static AppStatusPillVariant StatusVariant(ExamStatus status) => status.ToPillVariant();
}
