namespace Exam.WebApp.Components.Pages;

public partial class Exams : PageBase
{
    private IReadOnlyCollection<ExamDto>? exams;
    private Dictionary<string, ExamResultSummaryDto> myAttempts = new();
    private string? startingExamId;

    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () =>
        {
            var examsTask = Api.GetAvailableExamsAsync(1, 100);
            var historyTask = Api.GetMyExamHistoryAsync(1, 100);
            await Task.WhenAll(examsTask, historyTask);

            exams = examsTask.Result.Items;
            myAttempts = historyTask.Result.Items
                .GroupBy(a => a.ExamId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.ExamStartDate).First());
        }, "Không tải được danh sách đề thi");

    private async Task StartAsync(string examId)
    {
        startingExamId = examId;
        try
        {
            await ExecuteAsync(async () =>
            {
                var attempt = await Api.StartExamAsync(examId);
                Navigation.NavigateTo($"/exams/take/{attempt.Id}");
            }, "Không thể bắt đầu làm bài");
        }
        finally
        {
            startingExamId = null;
        }
    }
}
