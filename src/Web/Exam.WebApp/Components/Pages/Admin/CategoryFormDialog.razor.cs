using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class CategoryFormDialog : FormDialogBase
{
    [Parameter]
    public CategoryRequest Model { get; set; } = new("", "");

    private string name = "";
    private string urlPath = "";

    protected override void OnInitialized()
    {
        name = Model.Name;
        urlPath = Model.UrlPath;
    }

    private async Task Submit()
    {
        if (!await ValidateAsync())
            return;

        MudDialog.Close(DialogResult.Ok(new CategoryRequest(name, urlPath)));
    }
}
