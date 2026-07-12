using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

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

        MudDialog.Close(DialogResult.Ok(request));
    }
}
