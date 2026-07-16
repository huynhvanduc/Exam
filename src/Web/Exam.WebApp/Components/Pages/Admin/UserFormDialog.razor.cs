using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class UserFormDialog : FormDialogBase
{
    [Inject] private ExamApiClient Api { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private string firstName = "";
    private string lastName = "";
    private string email = "";
    private UserRole role = UserRole.Student;
    private bool isBusy;
    private bool copied;
    private CreateUserResponse? createdResponse;

    private void OnFirstNameChanged(string? value) => firstName = value ?? "";
    private void OnLastNameChanged(string? value) => lastName = value ?? "";
    private void OnEmailChanged(string? value) => email = value ?? "";

    private async Task Submit()
    {
        if (!await ValidateAsync())
            return;

        isBusy = true;
        await ExecuteAsync(async () =>
        {
            createdResponse = await Api.CreateUserAsync(new CreateUserRequest(email, firstName, lastName, role));
        }, "Tạo tài khoản thất bại");
        isBusy = false;
    }

    private async Task CopyPasswordAsync()
    {
        if (createdResponse == null)
            return;

        await JS.InvokeVoidAsync("appShell.copyToClipboard", createdResponse.GeneratedPassword);
        copied = true;
    }

    private void Done() => Dialog.Close(AppDialogResult.Ok(createdResponse));
}
