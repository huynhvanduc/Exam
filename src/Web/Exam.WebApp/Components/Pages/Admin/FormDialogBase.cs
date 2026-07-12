using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public abstract class FormDialogBase : ComponentBase
{
    [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = null!;

    protected MudForm form = null!;

    protected async Task<bool> ValidateAsync()
    {
        await form.ValidateAsync();
        return form.IsValid;
    }

    protected void Cancel() => MudDialog.Cancel();
}
