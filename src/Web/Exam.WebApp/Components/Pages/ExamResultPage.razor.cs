using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages;

public partial class ExamResultPage : PageBase
{
    [Parameter] public string AttemptId { get; set; } = "";
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private ExamResultDto? result;

    protected override Task OnInitializedAsync() =>
        ExecuteAsync(async () => result = await Api.GetExamResultAsync(AttemptId), "Không tải được kết quả");

    private static string AnswerCssClass(AnswerResultDto answer) =>
        answer.IsCorrect ? "correct-answer" : answer.UserChosen == true ? "wrong-chosen" : "";

    private void GoToHistory() => Navigation.NavigateTo("/my-history");
}
