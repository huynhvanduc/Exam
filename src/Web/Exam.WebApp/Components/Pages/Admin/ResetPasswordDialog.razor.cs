using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ResetPasswordDialog : FormDialogBase
{
    [Inject] private ExamApiClient Api { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public string ExternalId { get; set; } = null!;
    [Parameter] public string FullName { get; set; } = null!;
    [Parameter] public string Email { get; set; } = null!;

    private bool isBusy;
    private bool copied;
    private ResetUserPasswordResponse? result;

    private async Task Submit()
    {
        isBusy = true;
        await ExecuteAsync(async () =>
        {
            result = await Api.ResetUserPasswordAsync(ExternalId);
        }, "Đặt lại mật khẩu thất bại");
        isBusy = false;
    }

    private async Task CopyPasswordAsync()
    {
        if (result == null)
            return;

        await JS.InvokeVoidAsync("appShell.copyToClipboard", result.GeneratedPassword);
        copied = true;
    }

    private void Done() => Dialog.Close(AppDialogResult.Ok(result));
}
