using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ExamFormDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public string CategoryId { get; set; } = "";

    [Parameter]
    public ExamDto? Model { get; set; }

    private MudForm form = null!;
    private string name = "";
    private string shortDesc = "";
    private string content = "";
    private int durationMinutes = 30;
    private Level level = Level.Easy;
    private int minimumPassingScore = 5;
    private bool isTimeRestricted = true;

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
        }
    }

    private async Task Submit()
    {
        await form.ValidateAsync();
        if (!form.IsValid)
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

        MudDialog.Close(DialogResult.Ok(request));
    }

    private void Cancel() => MudDialog.Cancel();
}
