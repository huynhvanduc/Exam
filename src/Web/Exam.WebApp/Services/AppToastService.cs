using Microsoft.JSInterop;

namespace Exam.WebApp.Services;

public enum AppSeverity { Normal, Success, Warning, Error }

public interface IAppToastService
{
    void Add(string message, AppSeverity severity = AppSeverity.Normal);
}

public sealed class AppToastService(IJSRuntime js) : IAppToastService
{
    public void Add(string message, AppSeverity severity = AppSeverity.Normal)
    {
        var cssClass = severity switch
        {
            AppSeverity.Success => "success",
            AppSeverity.Warning => "warning",
            AppSeverity.Error => "error",
            _ => ""
        };
        _ = js.InvokeVoidAsync("appShell.toast", message, cssClass);
    }
}
