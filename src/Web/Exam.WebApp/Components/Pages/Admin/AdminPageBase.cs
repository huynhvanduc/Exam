using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Exam.WebApp.Components.Pages.Admin;

public abstract class AdminPageBase : ComponentBase
{
    private const string ConnectivityErrorMessage = "Không kết nối được máy chủ, vui lòng thử lại sau.";

    [Inject] protected ExamApiClient Api { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;

    protected async Task ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null)
    {
        try
        {
            await action();
            if (successMessage != null)
                Snackbar.Add(successMessage, Severity.Success);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"{errorPrefix}: {ex.Message}", Severity.Error);
        }
        catch (Exception ex) when (IsConnectivityFailure(ex))
        {
            Snackbar.Add($"{errorPrefix}: {ConnectivityErrorMessage}", Severity.Error);
        }
    }

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

    protected async Task<TableData<TItem>> LoadTableDataAsync<TItem>(Func<Task<TableData<TItem>>> load, string errorPrefix)
    {
        try
        {
            return await load();
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"{errorPrefix}: {ex.Message}", Severity.Error);
            return new TableData<TItem> { Items = [], TotalItems = 0 };
        }
        catch (Exception ex) when (IsConnectivityFailure(ex))
        {
            Snackbar.Add($"{errorPrefix}: {ConnectivityErrorMessage}", Severity.Error);
            return new TableData<TItem> { Items = [], TotalItems = 0 };
        }
    }

    private static bool IsConnectivityFailure(Exception ex) =>
        ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException;
}
