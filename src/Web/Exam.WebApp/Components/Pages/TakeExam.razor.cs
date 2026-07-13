using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages;

public partial class TakeExam : PageBase, IDisposable
{
    [Parameter] public string AttemptId { get; set; } = "";

    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private ExamAttemptDto? attempt;
    private readonly Dictionary<string, HashSet<string>> selections = new();
    private System.Timers.Timer? timer;
    private TimeSpan? remaining;

    private int AnsweredCount => selections.Count(kvp => kvp.Value.Count > 0);

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() => ExecuteAsync(async () =>
    {
        var status = await Api.GetExamAttemptStatusAsync(AttemptId);
        if (status.Finished || status.Attempt == null)
        {
            Navigation.NavigateTo($"/exams/result/{AttemptId}", replace: true);
            return;
        }

        attempt = status.Attempt;
        selections.Clear();
        foreach (var selection in attempt.SelectedAnswers)
            selections[selection.QuestionId] = selection.SelectedAnswerIds.ToHashSet();

        if (attempt.Deadline.HasValue)
            StartTimer(attempt.Deadline.Value);
    }, "Không tải được đề thi");

    private void StartTimer(DateTime deadline)
    {
        UpdateRemaining(deadline);
        timer = new System.Timers.Timer(1000);
        timer.Elapsed += (_, _) => InvokeAsync(async () =>
        {
            UpdateRemaining(deadline);
            StateHasChanged();
            if (remaining <= TimeSpan.Zero)
            {
                timer?.Stop();
                await AutoSubmitAsync();
            }
        });
        timer.Start();
    }

    private void UpdateRemaining(DateTime deadline) => remaining = deadline - DateTime.UtcNow;

    private async Task AutoSubmitAsync()
    {
        await ExecuteAsync(() => Api.FinishExamAsync(AttemptId), "Hết giờ, tự động nộp bài thất bại");
        Navigation.NavigateTo($"/exams/result/{AttemptId}", replace: true);
    }

    private string? SingleSelected(string questionId) =>
        selections.TryGetValue(questionId, out var set) ? set.FirstOrDefault() : null;

    private bool IsSelected(string questionId, string answerId) =>
        selections.TryGetValue(questionId, out var set) && set.Contains(answerId);

    private Task OnSingleAnswerChanged(string questionId, string answerId)
    {
        selections[questionId] = [answerId];
        return SaveAnswerAsync(questionId);
    }

    private Task OnMultiAnswerChanged(string questionId, string answerId, bool value)
    {
        if (!selections.TryGetValue(questionId, out var set))
        {
            set = [];
            selections[questionId] = set;
        }

        if (value)
            set.Add(answerId);
        else
            set.Remove(answerId);

        return SaveAnswerAsync(questionId);
    }

    private Task SaveAnswerAsync(string questionId) => ExecuteAsync(async () =>
    {
        var ids = selections.TryGetValue(questionId, out var set) ? set.ToList() : [];
        var result = await Api.RecordAnswerAsync(AttemptId, questionId, ids);
        if (result.Finished)
            Navigation.NavigateTo($"/exams/result/{AttemptId}", replace: true);
    }, "Không lưu được câu trả lời");

    private async Task SubmitAsync()
    {
        var confirmed = await DialogService.ShowMessageBoxAsync("Xác nhận nộp bài",
            "Bạn có chắc muốn nộp bài? Không thể chỉnh sửa sau khi nộp.", yesText: "Nộp bài", cancelText: "Huỷ");
        if (confirmed != true)
            return;

        await ExecuteAsync(async () =>
        {
            await Api.FinishExamAsync(AttemptId);
            Navigation.NavigateTo($"/exams/result/{AttemptId}", replace: true);
        }, "Nộp bài thất bại");
    }

    private static string FormatRemaining(TimeSpan ts) =>
        ts.TotalHours >= 1 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");

    public void Dispose() => timer?.Dispose();
}
