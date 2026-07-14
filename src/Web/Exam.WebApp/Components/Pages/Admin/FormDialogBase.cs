using Exam.WebApp.Components.UI;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages.Admin;

public abstract class FormDialogBase : ComponentBase
{
    [CascadingParameter] protected IAppDialogInstance Dialog { get; set; } = null!;
    [Inject] protected IApiErrorHandler ErrorHandler { get; set; } = null!;

    protected AppForm form = null!;

    protected Task<bool> ValidateAsync() => form.ValidateAsync();

    protected void Cancel() => Dialog.Cancel();

    protected Task ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null) =>
        ErrorHandler.ExecuteAsync(action, errorPrefix, successMessage);
}
