using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Exam.WebApp.Components.Pages;

public partial class TakeExam : PageBase, IDisposable
{
    [Parameter] public string AttemptId { get; set; } = "";

    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private ExamAttemptDto? attempt;
    private List<ExamAttemptQuestionDto> questions = [];
    private readonly Dictionary<string, HashSet<string>> selections = new();
    private readonly HashSet<string> marked = [];
    private int currentIndex;
    private bool showSubmitModal;
    private bool navigatorOpen;
    private System.Timers.Timer? timer;
    private TimeSpan? remaining;
    private bool dirtyGuardOn;

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
        questions = attempt.Questions.ToList();
        selections.Clear();
        foreach (var selection in attempt.SelectedAnswers)
            selections[selection.QuestionId] = selection.SelectedAnswerIds.ToHashSet();

        if (attempt.Deadline.HasValue)
            StartTimer(attempt.Deadline.Value);
    }, "Không tải được đề thi");

    // JS interop chỉ được phép gọi từ OnAfterRenderAsync trở đi (component đã interactive, có circuit
    // SignalR) - gọi trong OnInitializedAsync/LoadAsync ném InvalidOperationException vì lúc đó trang còn
    // đang ở giai đoạn prerender tĩnh (xảy ra với MỌI lần F5 reload). Bật cờ đúng 1 lần khi đã có attempt.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!dirtyGuardOn && attempt != null)
        {
            dirtyGuardOn = true;
            // Cảnh báo trước khi đóng tab/refresh giữa lúc đang thi - trước đây học viên có thể vô tình
            // đóng tab mà không được nhắc gì, mất phiên làm bài đang xem (đáp án đã lưu vẫn còn).
            await JS.InvokeVoidAsync("appShell.setDirtyGuard", true);
        }
    }

    private async Task LeaveExamAsync(string url)
    {
        dirtyGuardOn = false;
        await JS.InvokeVoidAsync("appShell.setDirtyGuard", false);
        Navigation.NavigateTo(url, replace: true);
    }

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
        await LeaveExamAsync($"/exams/result/{AttemptId}");
    }

    private bool IsAnswered(string questionId) => selections.TryGetValue(questionId, out var set) && set.Count > 0;

    private bool IsSelected(string questionId, string answerId) =>
        selections.TryGetValue(questionId, out var set) && set.Contains(answerId);

    private Task OnAnswerClicked(ExamAttemptQuestionDto question, string answerId)
    {
        if (question.QuestionType == QuestionType.SingleSelection)
        {
            selections[question.Id] = [answerId];
        }
        else
        {
            if (!selections.TryGetValue(question.Id, out var set))
            {
                set = [];
                selections[question.Id] = set;
            }

            if (!set.Add(answerId))
                set.Remove(answerId);
        }

        return SaveAnswerAsync(question.Id);
    }

    private Task SaveAnswerAsync(string questionId) => ExecuteAsync(async () =>
    {
        var ids = selections.TryGetValue(questionId, out var set) ? set.ToList() : [];
        var result = await Api.RecordAnswerAsync(AttemptId, questionId, ids);
        if (result.Finished)
            await LeaveExamAsync($"/exams/result/{AttemptId}");
    }, "Không lưu được câu trả lời");

    private void GoTo(int index)
    {
        currentIndex = Math.Clamp(index, 0, questions.Count - 1);
        navigatorOpen = false;
    }

    private void ToggleMark(string questionId)
    {
        if (!marked.Add(questionId))
            marked.Remove(questionId);
    }

    private void OnNextClicked()
    {
        if (currentIndex < questions.Count - 1)
            currentIndex++;
        else
            OpenSubmitModal();
    }

    private void OpenSubmitModal() => showSubmitModal = true;

    private int UnansweredCount() => questions.Count(q => !IsAnswered(q.Id));

    private string UnansweredListText()
    {
        var nums = questions.Select((q, i) => (q, i)).Where(x => !IsAnswered(x.q.Id)).Select(x => x.i + 1).ToList();
        return nums.Count > 0 ? "Câu " + string.Join(", ", nums) : "Không có";
    }

    private string MarkedListText()
    {
        var nums = questions.Select((q, i) => (q, i)).Where(x => marked.Contains(x.q.Id)).Select(x => x.i + 1).ToList();
        return nums.Count > 0 ? "Câu " + string.Join(", ", nums) : "Không có";
    }

    private async Task SubmitAsync()
    {
        showSubmitModal = false;
        await ExecuteAsync(async () =>
        {
            await Api.FinishExamAsync(AttemptId);
            await LeaveExamAsync($"/exams/result/{AttemptId}");
        }, "Nộp bài thất bại");
    }

    private static string FormatRemaining(TimeSpan ts) =>
        ts.TotalHours >= 1 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");

    public void Dispose()
    {
        timer?.Dispose();

        if (!dirtyGuardOn)
            return;

        dirtyGuardOn = false;

        // Dọn guard khi rời trang bằng bất kỳ cách nào (kể cả điều hướng nội bộ trong app) - tránh guard còn
        // sót "true" làm cảnh báo nhầm khi người dùng đóng tab ở một trang khác về sau. Dispose() không thể
        // await, và lúc này circuit SignalR có thể đã ngắt (JSDisconnectedException) - "quan sát" lỗi qua
        // ContinueWith thay vì try/catch quanh call fire-and-forget (try/catch ở đây sẽ không bắt được gì,
        // vì exception xảy ra bên trong Task chứ không phải ngay tại lệnh gọi).
        _ = JS.InvokeVoidAsync("appShell.setDirtyGuard", false).AsTask()
            .ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
