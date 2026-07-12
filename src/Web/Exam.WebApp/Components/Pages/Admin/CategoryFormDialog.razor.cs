using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class CategoryFormDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public CategoryRequest Model { get; set; } = new("", "");

    private MudForm form = null!;
    private string name = "";
    private string urlPath = "";

    protected override void OnInitialized()
    {
        name = Model.Name;
        urlPath = Model.UrlPath;
    }

    private async Task Submit()
    {
        await form.ValidateAsync();
        if (!form.IsValid)
            return;

        MudDialog.Close(DialogResult.Ok(new CategoryRequest(name, urlPath)));
    }

    private void Cancel() => MudDialog.Cancel();
}
