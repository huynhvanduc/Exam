using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ClassFormDialog : FormDialogBase
{
    [Parameter]
    public CreateClassRoomRequest Model { get; set; } = new("");

    private string name = "";

    protected override void OnInitialized()
    {
        name = Model.Name;
    }

    private async Task Submit()
    {
        if (!await ValidateAsync())
            return;

        MudDialog.Close(DialogResult.Ok(new CreateClassRoomRequest(name)));
    }
}
