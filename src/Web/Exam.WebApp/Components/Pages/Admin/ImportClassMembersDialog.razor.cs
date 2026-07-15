using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ImportClassMembersDialog : FormDialogBase
{
    private const int MaxPreviewRows = 50;

    [Inject] private ExamApiClient Api { get; set; } = null!;

    [Parameter] public string ClassId { get; set; } = null!;

    private IBrowserFile? selectedFile;
    private byte[]? fileBytes;
    private ImportClassMembersResultDto? previewResult;
    private bool isBusy;

    // Chọn file xong là tự động xem trước ngay (không cần nút riêng) - an toàn vì đây là dry-run,
    // không ghi DB, giúp admin thấy ngay nội dung sắp nhập mà không tốn thêm 1 lượt bấm.
    private async Task OnFileSelectedAsync(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        previewResult = null;
        fileBytes = null;

        if (selectedFile != null)
            await PreviewAsync();
    }

    private async Task PreviewAsync()
    {
        if (selectedFile == null)
            return;

        isBusy = true;
        await ExecuteAsync(async () =>
        {
            // IBrowserFile.OpenReadStream() chỉ đáng tin cậy khi đọc 1 LẦN DUY NHẤT trong Blazor Server -
            // gọi lại lần 2 (cho bước Xác nhận) làm rớt circuit SignalR. Đọc hẳn vào bộ nhớ ở đây rồi
            // dùng lại byte[] cho cả 2 bước.
            await using var stream = selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();

            previewResult = await Api.ImportClassMembersAsync(ClassId, new MemoryStream(fileBytes), selectedFile.Name, dryRun: true);
        }, "Xem trước thất bại");
        isBusy = false;
    }

    private async Task ConfirmAsync()
    {
        if (fileBytes == null || selectedFile == null)
            return;

        isBusy = true;
        await ExecuteAsync(async () =>
        {
            var result = await Api.ImportClassMembersAsync(ClassId, new MemoryStream(fileBytes), selectedFile.Name, dryRun: false);
            Dialog.Close(AppDialogResult.Ok(result));
        }, "Nhập danh sách thất bại");
        isBusy = false;
    }
}
