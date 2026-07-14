using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Exam.WebApp.Components.Layout;

public partial class MainLayout
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private void GoToLogin() => Navigation.GoToLogin();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Gọi mỗi lần render (không chỉ firstRender) vì AuthorizeView có thể resolve bất đồng bộ -
        // sidebar/toggle chỉ thực sự có trong DOM sau khi Authorized render xong. shell.js tự chống
        // gắn listener trùng qua dataset.wired nên gọi lại nhiều lần vẫn an toàn.
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
