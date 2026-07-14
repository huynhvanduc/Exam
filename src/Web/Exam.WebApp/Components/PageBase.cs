using Exam.WebApp.Components.UI;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components;

public abstract class PageBase : ComponentBase
{
    [Inject] protected ExamApiClient Api { get; set; } = null!;
    [Inject] protected IAppToastService Toast { get; set; } = null!;
    [Inject] protected IApiErrorHandler ErrorHandler { get; set; } = null!;

    protected Task ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null) =>
        ErrorHandler.ExecuteAsync(action, errorPrefix, successMessage);

    protected Task<AppTableData<TItem>> LoadAppTableDataAsync<TItem>(Func<Task<AppTableData<TItem>>> load, string errorPrefix) =>
        ErrorHandler.LoadAppTableDataAsync(load, errorPrefix);
}
