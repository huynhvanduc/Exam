using Exam.WebApp.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public abstract class AdminPageBase : PageBase
{
    [Inject] protected IDialogService DialogService { get; set; } = null!;

    protected async Task ConfirmAndExecuteAsync(string title, string message, Func<Task> action,
        string errorPrefix, string? successMessage = null, string yesText = "Xoá", string cancelText = "Huỷ")
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(title, message, yesText: yesText, cancelText: cancelText);
        if (confirmed != true)
            return;

        await ExecuteAsync(action, errorPrefix, successMessage);
    }

    protected async Task<TRequest?> ShowFormDialogAsync<TDialog, TRequest>(string title, DialogParameters<TDialog> parameters,
        DialogOptions? options = null)
        where TDialog : ComponentBase
        where TRequest : class
    {
        var dialog = await DialogService.ShowAsync<TDialog>(title, parameters, options);
        var result = await dialog.Result;
        return result is { Canceled: false, Data: TRequest data } ? data : null;
    }
}
