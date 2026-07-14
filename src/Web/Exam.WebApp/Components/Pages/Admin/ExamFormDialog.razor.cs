using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ExamFormDialog : FormDialogBase
{
    [Parameter]
    public string CategoryId { get; set; } = "";

    [Parameter]
    public ExamDto? Model { get; set; }

    private string name = "";
    private string shortDesc = "";
    private string content = "";
    private int durationMinutes = 30;
    private Level level = Level.Easy;
    private int minimumPassingScore = 5;
    private bool isTimeRestricted = true;

    private bool enableAvailability;
    private DateTime? availableFrom;
    private DateTime? availableTo;

    private bool enableNegativeMarking;
    private decimal negativeMarkingRatio = 0.25M;

    private bool enablePool;
    private int poolQuestionCount = 10;

    private bool enableMaxAttempts;
    private int maxAttempts = 1;

    // Chỉ liên quan khi sửa đề thi đã tồn tại (Model != null) - đề mới tạo luôn Draft, chưa có câu hỏi.
    private bool isLocked => Model != null && Model.Status != ExamStatus.Draft;
    private bool isArchived => Model != null && Model.Status == ExamStatus.Archived;
    private bool hasFixedQuestions => Model != null
        && Model.QuestionSelectionMode != QuestionSelectionMode.Pool && Model.QuestionIds.Count > 0;

    protected override void OnInitialized()
    {
        if (Model != null)
        {
            name = Model.Name;
            shortDesc = Model.ShortDesc;
            content = Model.Content;
            durationMinutes = (int)Model.Duration.TotalMinutes;
            level = Model.Level;
            minimumPassingScore = Model.MinimumPassingScore;
            isTimeRestricted = Model.IsTimeRestricted;

            enableAvailability = Model.AvailableFrom.HasValue || Model.AvailableTo.HasValue;
            availableFrom = Model.AvailableFrom;
            availableTo = Model.AvailableTo;

            enableNegativeMarking = Model.NegativeMarkingRatio > 0;
            if (Model.NegativeMarkingRatio > 0)
                negativeMarkingRatio = Model.NegativeMarkingRatio;

            enablePool = Model.QuestionSelectionMode == QuestionSelectionMode.Pool;
            if (Model.PoolQuestionCount > 0)
                poolQuestionCount = Model.PoolQuestionCount;

            enableMaxAttempts = Model.MaxAttempts.HasValue;
            if (Model.MaxAttempts.HasValue)
                maxAttempts = Model.MaxAttempts.Value;
        }
    }

    private async Task Submit()
    {
        if (!await ValidateAsync())
            return;

        var request = new ExamRequest(
            name,
            shortDesc,
            content,
            TimeSpan.FromMinutes(durationMinutes),
            level,
            CategoryId,
            isTimeRestricted,
            minimumPassingScore);

        // Bỏ chọn (tắt switch) nghĩa là "không đổi gì" ở đây, không tự xóa cấu hình đã có khi sửa -
        // muốn xóa hẳn Lịch phát hành / trừ điểm thì vẫn có thể vào Chi tiết đề thi.
        var availability = enableAvailability ? new ScheduleExamAvailabilityRequest(availableFrom, availableTo) : null;
        var negativeMarking = enableNegativeMarking ? new ConfigureNegativeMarkingRequest(negativeMarkingRatio) : null;
        var pool = enablePool && !hasFixedQuestions ? new ConfigureQuestionPoolRequest(CategoryId, poolQuestionCount) : null;
        var maxAttemptsRequest = enableMaxAttempts ? new ConfigureMaxAttemptsRequest(maxAttempts) : null;

        Dialog.Close(AppDialogResult.Ok(new ExamFormResult(request, availability, negativeMarking, pool, maxAttemptsRequest)));
    }
}
