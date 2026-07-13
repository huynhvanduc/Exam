using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ImportQuestionsDialog : FormDialogBase
{
    private const int MaxPreviewRows = 50;

    [Inject] private ExamApiClient Api { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private IBrowserFile? selectedFile;
    private byte[]? fileBytes;
    private ImportQuestionsResultDto? previewResult;
    private bool isBusy;

    // Chọn file xong là tự động xem trước ngay (không cần nút riêng) - an toàn vì đây là dry-run,
    // không ghi DB, giúp admin thấy ngay nội dung sắp nhập mà không tốn thêm 1 lượt bấm.
    private async Task OnFileSelectedAsync(IBrowserFile file)
    {
        selectedFile = file;
        previewResult = null;
        fileBytes = null;

        if (file != null)
            await PreviewAsync();
    }

    private async Task PreviewAsync()
    {
        if (selectedFile == null)
            return;

        isBusy = true;
        try
        {
            // IBrowserFile.OpenReadStream() chỉ đáng tin cậy khi đọc 1 LẦN DUY NHẤT trong Blazor Server -
            // gọi lại lần 2 (cho bước Xác nhận) làm rớt circuit SignalR ("Cannot send data if the connection
            // is not in the 'Connected' State"). Đọc hẳn vào bộ nhớ ở đây rồi dùng lại byte[] cho cả 2 bước.
            await using var stream = selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();

            previewResult = await Api.ImportQuestionsAsync(new MemoryStream(fileBytes), selectedFile.Name, dryRun: true);
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Xem trước thất bại: {ex.Message}", Severity.Error);
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task ConfirmAsync()
    {
        if (fileBytes == null || selectedFile == null)
            return;

        isBusy = true;
        try
        {
            var result = await Api.ImportQuestionsAsync(new MemoryStream(fileBytes), selectedFile.Name, dryRun: false);
            MudDialog.Close(DialogResult.Ok(result));
        }
        catch (ExamApiException ex)
        {
            Snackbar.Add($"Nhập câu hỏi thất bại: {ex.Message}", Severity.Error);
        }
        finally
        {
            isBusy = false;
        }
    }

    private static string Truncate(string content) => content.Length <= 90 ? content : content[..90] + "…";
}
