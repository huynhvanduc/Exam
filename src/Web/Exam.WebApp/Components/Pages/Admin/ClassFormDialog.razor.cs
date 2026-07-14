using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

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

        Dialog.Close(AppDialogResult.Ok(new CreateClassRoomRequest(name)));
    }
}
