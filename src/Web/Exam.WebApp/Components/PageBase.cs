using Exam.WebApp.Components.UI;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Exam.WebApp.Components;

public abstract class PageBase : ComponentBase
{
    private const string ConnectivityErrorMessage = "Không kết nối được máy chủ, vui lòng thử lại sau.";

    [Inject] protected ExamApiClient Api { get; set; } = null!;
    [Inject] protected IAppToastService Toast { get; set; } = null!;

    protected async Task ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null)
    {
        try
        {
            await action();
            if (successMessage != null)
                Toast.Add(successMessage, AppSeverity.Success);
        }
        catch (ExamApiException ex)
        {
            Toast.Add($"{errorPrefix}: {ex.Message}", AppSeverity.Error);
        }
        catch (Exception ex) when (IsConnectivityFailure(ex))
        {
            Toast.Add($"{errorPrefix}: {ConnectivityErrorMessage}", AppSeverity.Error);
        }
    }

    protected async Task<AppTableData<TItem>> LoadAppTableDataAsync<TItem>(Func<Task<AppTableData<TItem>>> load, string errorPrefix)
    {
        try
        {
            return await load();
        }
        catch (ExamApiException ex)
        {
            Toast.Add($"{errorPrefix}: {ex.Message}", AppSeverity.Error);
            return new AppTableData<TItem> { Items = [], TotalItems = 0 };
        }
        catch (Exception ex) when (IsConnectivityFailure(ex))
        {
            Toast.Add($"{errorPrefix}: {ConnectivityErrorMessage}", AppSeverity.Error);
            return new AppTableData<TItem> { Items = [], TotalItems = 0 };
        }
    }

    protected static bool IsConnectivityFailure(Exception ex) =>
        ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException;
}
