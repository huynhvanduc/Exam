using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ExamDetail : AdminPageBase
{
    [Parameter]
    public string Id { get; set; } = "";

    private ExamDto? exam;
    private IReadOnlyCollection<QuestionDto>? categoryQuestions;
    private IReadOnlyCollection<ClassRoomDto> allClasses = [];
    private DateTime? availableFrom;
    private DateTime? availableTo;
    private decimal negativeMarkingRatio;
    private int poolQuestionCount = 10;
    private bool limitMaxAttempts;
    private int maxAttempts = 1;
    private string? selectedClassId;
    private MudTable<ExamResultAdminListItemDto>? resultsTable;

    private IReadOnlyCollection<ClassRoomDto> unassignedClasses =>
        allClasses.Where(c => exam != null && !exam.AssignedClassIds.Contains(c.Id)).ToList();

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private Task LoadAsync() => ExecuteAsync(async () =>
    {
        exam = await Api.GetExamByIdAsync(Id);
        availableFrom = exam.AvailableFrom;
        availableTo = exam.AvailableTo;
        negativeMarkingRatio = exam.NegativeMarkingRatio;
        poolQuestionCount = exam.PoolQuestionCount > 0 ? exam.PoolQuestionCount : 10;
        limitMaxAttempts = exam.MaxAttempts.HasValue;
        maxAttempts = exam.MaxAttempts ?? 1;
        allClasses = await Api.GetClassesAsync();
        selectedClassId = null;

        if (exam.QuestionSelectionMode == QuestionSelectionMode.Fixed)
            categoryQuestions = await Api.GetQuestionsByCategoryAsync(exam.CategoryId);
    }, "Không tải được đề thi");

    private string ClassName(string classId) => allClasses.FirstOrDefault(c => c.Id == classId)?.Name ?? "(Lớp không còn tồn tại)";

    private Task AssignClassAsync() => ExecuteAsync(async () =>
    {
        exam = await Api.AssignExamToClassAsync(Id, selectedClassId!);
        selectedClassId = null;
    }, "Gán lớp thất bại", "Đã gán lớp.");

    private Task UnassignClassAsync(string classId) => ExecuteAsync(async () =>
    {
        exam = await Api.UnassignExamFromClassAsync(Id, classId);
    }, "Bỏ gán thất bại", "Đã bỏ gán lớp.");

    private Task ToggleQuestionAsync(string questionId, bool add) => ExecuteAsync(async () =>
    {
        exam = add
            ? await Api.AddQuestionToExamAsync(Id, questionId)
            : await Api.RemoveQuestionFromExamAsync(Id, questionId);
    }, "Thao tác thất bại", add ? "Đã thêm câu hỏi." : "Đã bỏ câu hỏi.");

    private Task SavePoolAsync() => ExecuteAsync(async () =>
    {
        exam = await Api.ConfigureQuestionPoolAsync(Id, new ConfigureQuestionPoolRequest(exam!.CategoryId, poolQuestionCount));
    }, "Lưu thất bại", "Đã lưu cấu hình pool.");

    private Task SaveAvailabilityAsync() => ExecuteAsync(async () =>
    {
        exam = await Api.ScheduleExamAvailabilityAsync(Id, new ScheduleExamAvailabilityRequest(availableFrom, availableTo));
    }, "Lưu thất bại", "Đã lưu lịch phát hành.");

    private Task SaveNegativeMarkingAsync() => ExecuteAsync(async () =>
    {
        exam = await Api.ConfigureNegativeMarkingAsync(Id, new ConfigureNegativeMarkingRequest(negativeMarkingRatio));
    }, "Lưu thất bại", "Đã lưu.");

    private Task SaveMaxAttemptsAsync() => ExecuteAsync(async () =>
    {
        exam = await Api.ConfigureMaxAttemptsAsync(Id, new ConfigureMaxAttemptsRequest(limitMaxAttempts ? maxAttempts : null));
    }, "Lưu thất bại", "Đã lưu.");

    private Task PublishAsync() => ExecuteAsync(async () =>
    {
        exam = await Api.PublishExamAsync(Id);
    }, "Xuất bản thất bại", "Đã xuất bản.");

    private Task UnpublishAsync() => ExecuteAsync(async () =>
    {
        exam = await Api.UnpublishExamAsync(Id);
    }, "Thao tác thất bại", "Đã chuyển về Nháp.");

    private Task ArchiveAsync() => ConfirmAndExecuteAsync(
        "Xác nhận lưu trữ", $"Lưu trữ đề thi '{exam!.Name}'? Không thể hoàn tác.",
        async () => exam = await Api.ArchiveExamAsync(Id),
        "Lưu trữ thất bại", "Đã lưu trữ.", yesText: "Lưu trữ");

    private Task<TableData<ExamResultAdminListItemDto>> LoadResultsServerData(TableState state, CancellationToken cancellationToken) =>
        LoadTableDataAsync(async () =>
        {
            var result = await Api.GetExamResultsByExamAsync(Id, state.Page + 1, state.PageSize, cancellationToken);
            return new TableData<ExamResultAdminListItemDto> { Items = result.Items, TotalItems = (int)result.TotalCount };
        }, "Không tải được kết quả thi");

    private static string Truncate(string content) => content.Length <= 100 ? content : content[..100] + "…";
}
