using Exam.WebApp.Components;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages.Admin;

public abstract class AdminPageBase : PageBase
{
    [Inject] protected IAppDialogService DialogService { get; set; } = null!;

    // Khớp đúng OwnershipGuard.EnsureOwnerOrAdmin ở backend (Admin luôn được, Instructor chỉ được với
    // tài nguyên do chính mình tạo) - tính trước ở UI để ẩn/khoá các hành động chắc chắn sẽ bị 403 thay
    // vì để người dùng bấm rồi mới thấy toast lỗi. Dùng chung cho Exams/Questions/Classes.
    protected static bool CanManage(UserDto? currentUser, string ownerUserId) =>
        currentUser != null && (currentUser.Role == UserRole.Admin || currentUser.ExternalId == ownerUserId);

    protected async Task ConfirmAndExecuteAsync(string title, string message, Func<Task> action,
        string errorPrefix, string? successMessage = null, string yesText = "Xoá", string cancelText = "Huỷ")
    {
        var confirmed = await DialogService.ShowConfirmAsync(title, message, yesText, cancelText);
        if (!confirmed)
            return;

        await ExecuteAsync(action, errorPrefix, successMessage);
    }

    protected async Task<TRequest?> ShowFormDialogAsync<TDialog, TRequest>(string title, Dictionary<string, object> parameters,
        AppDialogOptions? options = null)
        where TDialog : ComponentBase
        where TRequest : class
    {
        var result = await DialogService.ShowAsync<TDialog>(title, parameters, options);
        return !result.Canceled && result.Data is TRequest data ? data : null;
    }
}
