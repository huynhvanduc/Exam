using Exam.WebApp.Components.UI;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Exam.WebApp.Services;

public interface IApiErrorHandler
{
    Task ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null);

    Task<AppTableData<TItem>> LoadAppTableDataAsync<TItem>(Func<Task<AppTableData<TItem>>> load, string errorPrefix);
}

public sealed class ApiErrorHandler(IAppToastService toast) : IApiErrorHandler
{
    private const string ConnectivityErrorMessage = "Không kết nối được máy chủ, vui lòng thử lại sau.";

    public async Task ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null)
    {
        try
        {
            await action();
            if (successMessage != null)
                toast.Add(successMessage, AppSeverity.Success);
        }
        catch (ExamApiException ex)
        {
            toast.Add($"{errorPrefix}: {ex.Message}", AppSeverity.Error);
        }
        catch (Exception ex) when (IsConnectivityFailure(ex))
        {
            toast.Add($"{errorPrefix}: {ConnectivityErrorMessage}", AppSeverity.Error);
        }
    }

    public async Task<AppTableData<TItem>> LoadAppTableDataAsync<TItem>(Func<Task<AppTableData<TItem>>> load, string errorPrefix)
    {
        try
        {
            return await load();
        }
        catch (ExamApiException ex)
        {
            toast.Add($"{errorPrefix}: {ex.Message}", AppSeverity.Error);
            return new AppTableData<TItem> { Items = [], TotalItems = 0 };
        }
        catch (Exception ex) when (IsConnectivityFailure(ex))
        {
            toast.Add($"{errorPrefix}: {ConnectivityErrorMessage}", AppSeverity.Error);
            return new AppTableData<TItem> { Items = [], TotalItems = 0 };
        }
    }

    private static bool IsConnectivityFailure(Exception ex) =>
        ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException;
}
