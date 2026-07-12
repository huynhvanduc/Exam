using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ExamDetail : ComponentBase
{
    [Parameter]
    public string Id { get; set; } = "";

    private ExamDto? exam;
    private IReadOnlyCollection<QuestionDto>? categoryQuestions;
    private DateTime? availableFrom;
    private DateTime? availableTo;
    private decimal negativeMarkingRatio;
    private int poolQuestionCount = 10;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            exam = await Api.GetExamByIdAsync(Id);
            availableFrom = exam.AvailableFrom;
            availableTo = exam.AvailableTo;
            negativeMarkingRatio = exam.NegativeMarkingRatio;
            poolQuestionCount = exam.PoolQuestionCount > 0 ? exam.PoolQuestionCount : 10;

            if (exam.QuestionSelectionMode == QuestionSelectionMode.Fixed)
                categoryQuestions = await Api.GetQuestionsByCategoryAsync(exam.CategoryId);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Không tải được đề thi: {ex.Message}", Severity.Error);
        }
    }

    private async Task ToggleQuestionAsync(string questionId, bool add)
    {
        try
        {
            exam = add
                ? await Api.AddQuestionToExamAsync(Id, questionId)
                : await Api.RemoveQuestionFromExamAsync(Id, questionId);
            Snackbar.Add(add ? "Đã thêm câu hỏi." : "Đã bỏ câu hỏi.", Severity.Success);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Thao tác thất bại: {ex.Message}", Severity.Error);
        }
    }

    private async Task SavePoolAsync()
    {
        try
        {
            exam = await Api.ConfigureQuestionPoolAsync(Id, new ConfigureQuestionPoolRequest(exam!.CategoryId, poolQuestionCount));
            Snackbar.Add("Đã lưu cấu hình pool.", Severity.Success);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Lưu thất bại: {ex.Message}", Severity.Error);
        }
    }

    private async Task SaveAvailabilityAsync()
    {
        try
        {
            exam = await Api.ScheduleExamAvailabilityAsync(Id, new ScheduleExamAvailabilityRequest(availableFrom, availableTo));
            Snackbar.Add("Đã lưu lịch phát hành.", Severity.Success);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Lưu thất bại: {ex.Message}", Severity.Error);
        }
    }

    private async Task SaveNegativeMarkingAsync()
    {
        try
        {
            exam = await Api.ConfigureNegativeMarkingAsync(Id, new ConfigureNegativeMarkingRequest(negativeMarkingRatio));
            Snackbar.Add("Đã lưu.", Severity.Success);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Lưu thất bại: {ex.Message}", Severity.Error);
        }
    }

    private async Task PublishAsync()
    {
        try
        {
            exam = await Api.PublishExamAsync(Id);
            Snackbar.Add("Đã xuất bản.", Severity.Success);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Xuất bản thất bại: {ex.Message}", Severity.Error);
        }
    }

    private async Task UnpublishAsync()
    {
        try
        {
            exam = await Api.UnpublishExamAsync(Id);
            Snackbar.Add("Đã chuyển về Nháp.", Severity.Success);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Thao tác thất bại: {ex.Message}", Severity.Error);
        }
    }

    private async Task ArchiveAsync()
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Xác nhận lưu trữ", $"Lưu trữ đề thi '{exam!.Name}'? Không thể hoàn tác.", yesText: "Lưu trữ", cancelText: "Huỷ");

        if (confirmed != true)
            return;

        try
        {
            exam = await Api.ArchiveExamAsync(Id);
            Snackbar.Add("Đã lưu trữ.", Severity.Success);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Lưu trữ thất bại: {ex.Message}", Severity.Error);
        }
    }

    private static string Truncate(string content) => content.Length <= 100 ? content : content[..100] + "…";

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
