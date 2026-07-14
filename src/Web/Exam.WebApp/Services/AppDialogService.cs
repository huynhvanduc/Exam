using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Services;

public sealed class AppDialogResult
{
    public bool Canceled { get; private init; }
    public object? Data { get; private init; }

    public static AppDialogResult Ok(object? data) => new() { Data = data };
    public static AppDialogResult Cancel() => new() { Canceled = true };
}

public sealed class AppDialogOptions
{
    public bool Wide { get; init; }
}

public interface IAppDialogInstance
{
    void Close(AppDialogResult result);
    void Cancel();
}

public sealed class AppDialogRequest(
    Type componentType, string title, Dictionary<string, object> parameters,
    AppDialogOptions options, TaskCompletionSource<AppDialogResult> completion)
{
    public Type ComponentType { get; } = componentType;
    public string Title { get; } = title;
    public Dictionary<string, object> Parameters { get; } = parameters;
    public AppDialogOptions Options { get; } = options;

    public IAppDialogInstance CreateInstance(Action onClosed) => new Instance(completion, onClosed);

    private sealed class Instance(TaskCompletionSource<AppDialogResult> completion, Action onClosed) : IAppDialogInstance
    {
        public void Close(AppDialogResult result)
        {
            completion.TrySetResult(result);
            onClosed();
        }

        public void Cancel()
        {
            completion.TrySetResult(AppDialogResult.Cancel());
            onClosed();
        }
    }
}

public sealed class AppConfirmRequest(string title, string message, string yesText, string cancelText, TaskCompletionSource<bool> completion)
{
    public string Title { get; } = title;
    public string Message { get; } = message;
    public string YesText { get; } = yesText;
    public string CancelText { get; } = cancelText;

    public void Resolve(bool confirmed) => completion.TrySetResult(confirmed);
}

public interface IAppDialogService
{
    event Action<AppDialogRequest>? DialogRequested;
    event Action<AppConfirmRequest>? ConfirmRequested;

    Task<AppDialogResult> ShowAsync<TDialog>(string title, Dictionary<string, object> parameters, AppDialogOptions? options = null)
        where TDialog : ComponentBase;

    Task<bool> ShowConfirmAsync(string title, string message, string yesText = "Xác nhận", string cancelText = "Huỷ");
}

public sealed class AppDialogService : IAppDialogService
{
    public event Action<AppDialogRequest>? DialogRequested;
    public event Action<AppConfirmRequest>? ConfirmRequested;

    public Task<AppDialogResult> ShowAsync<TDialog>(string title, Dictionary<string, object> parameters, AppDialogOptions? options = null)
        where TDialog : ComponentBase
    {
        var completion = new TaskCompletionSource<AppDialogResult>();
        var request = new AppDialogRequest(typeof(TDialog), title, parameters, options ?? new AppDialogOptions(), completion);
        DialogRequested?.Invoke(request);
        return completion.Task;
    }

    public Task<bool> ShowConfirmAsync(string title, string message, string yesText = "Xác nhận", string cancelText = "Huỷ")
    {
        var completion = new TaskCompletionSource<bool>();
        ConfirmRequested?.Invoke(new AppConfirmRequest(title, message, yesText, cancelText, completion));
        return completion.Task;
    }
}
