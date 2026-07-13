using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages;

public partial class ExamResultPage : PageBase
{
    [Parameter] public string AttemptId { get; set; } = "";

    private ExamResultDto? result;

    protected override Task OnInitializedAsync() =>
        ExecuteAsync(async () => result = await Api.GetExamResultAsync(AttemptId), "Không tải được kết quả");

    private static Color AnswerColor(AnswerResultDto answer) =>
        answer.IsCorrect ? Color.Success : answer.UserChosen == true ? Color.Error : Color.Default;
}
