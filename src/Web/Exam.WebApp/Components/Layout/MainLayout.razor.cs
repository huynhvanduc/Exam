using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Exam.WebApp.Components.Layout;

public partial class MainLayout
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private void GoToLogin() => Navigation.GoToLogin();

    // forceLoad: true bắt buộc - "/idp/Account/ChangePassword" được YARP proxy sang Identity.Server,
    // không thuộc route của Blazor Router; nếu để Blazor tự điều hướng (client-side interactive routing),
    // nó sẽ không khớp route nào và rơi vào CatchAllNotFound thay vì tải trang thật từ server.
    private void GoToChangePassword() => Navigation.NavigateTo("/idp/Account/ChangePassword", forceLoad: true);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JS.InvokeVoidAsync("appShell.initSidebarToggle");
    }

    private static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
