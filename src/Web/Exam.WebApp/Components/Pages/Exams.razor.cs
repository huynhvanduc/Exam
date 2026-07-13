namespace Exam.WebApp.Components.Pages;

public partial class Exams : PageBase
{
    private IReadOnlyCollection<ExamDto>? exams;
    private string? startingExamId;

    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () =>
        {
            var result = await Api.GetAvailableExamsAsync(1, 100);
            exams = result.Items;
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
