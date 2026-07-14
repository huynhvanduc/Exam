using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.UI;

public partial class AppDialogHost : ComponentBase, IDisposable
{
    private AppDialogRequest? currentDialog;
    private IAppDialogInstance? currentDialogInstance;
    private AppConfirmRequest? currentConfirm;

    protected override void OnInitialized()
    {
        DialogService.DialogRequested += OnDialogRequested;
        DialogService.ConfirmRequested += OnConfirmRequested;
    }

    private void OnDialogRequested(AppDialogRequest request)
    {
        currentDialog = request;
        currentDialogInstance = request.CreateInstance(CloseDialog);
        InvokeAsync(StateHasChanged);
    }

    private void OnConfirmRequested(AppConfirmRequest request)
    {
        currentConfirm = request;
        InvokeAsync(StateHasChanged);
    }

    private void CloseDialog()
    {
        currentDialog = null;
        currentDialogInstance = null;
        InvokeAsync(StateHasChanged);
    }

    private void ResolveConfirm(bool confirmed)
    {
        currentConfirm?.Resolve(confirmed);
        currentConfirm = null;
    }

    public void Dispose()
    {
        DialogService.DialogRequested -= OnDialogRequested;
        DialogService.ConfirmRequested -= OnConfirmRequested;
    }
}
