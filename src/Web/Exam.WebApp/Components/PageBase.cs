using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Exam.WebApp.Components;

public abstract class PageBase : ComponentBase
{
    private const string ConnectivityErrorMessage = "Không kết nối được máy chủ, vui lòng thử lại sau.";

    [Inject] protected ExamApiClient Api { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

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

    protected static bool IsConnectivityFailure(Exception ex) =>
        ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException;
}
