# Exam.WebApp Foundation Hardening (Hướng A) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Củng cố nền tảng dùng chung của `Exam.WebApp` (Blazor Server, khu vực Admin) qua 4 cải tiến thấp rủi ro — document cú pháp bind của UI kit, gộp helper `Truncate` bị trùng lặp 3 lần, gộp 1 chuẩn xử lý lỗi API duy nhất cho cả Page lẫn Dialog, và vá lỗ hổng UI-không-phản-chiếu-ownership đang tồn tại thật ở Questions.razor và Classes.razor — để fresher/junior sắp gia nhập có thể viết trang mới đúng chuẩn ngay từ đầu.

**Architecture:** Không đổi kiến trúc tổng thể (CQRS backend giữ nguyên, không đổi). Chỉ tái cấu trúc nội bộ tầng Blazor: tách `PageBase.ExecuteAsync` thành 1 service `IApiErrorHandler` dùng chung được bởi cả `PageBase` và `FormDialogBase`; tổng quát hoá pattern ownership-check đã có ở `Exams.razor.cs` (`CanManageSelected`) thành 1 helper tĩnh `AdminPageBase.CanManage(...)` áp dụng lại cho `Questions`/`Classes`.

**Tech Stack:** .NET 8, Blazor Server (InteractiveServer render mode), MediatR/CQRS ở backend (không đổi), Polly (circuit breaker/timeout) cho phát hiện lỗi kết nối, docker-compose để chạy trực tiếp và kiểm tra thủ công.

## Global Constraints

- Không viết unit test (quy ước dự án đã có từ trước cho `Exam.Domain`, áp dụng tương tự ở đây) — xác minh bằng `dotnet build` + thao tác tay trên app chạy qua docker-compose.
- Không đổi bất kỳ method signature công khai nào của `PageBase` (`ExecuteAsync`, `LoadAppTableDataAsync`) — mọi trang kế thừa hiện có (bao gồm `TakeExam.razor.cs`) không được sửa.
- Không đổi luật nghiệp vụ ownership ở backend (`OwnershipGuard.EnsureOwnerOrAdmin`) — chỉ thêm UI phản chiếu luật đã có, không đổi logic backend.
- Giữ nguyên độ dài `maxLength` hiện có ở từng call site của `Truncate` (100/90/80) — đây là quyết định hiển thị theo layout từng trang, không thống nhất về 1 con số.
- Spec gốc: `docs/superpowers/specs/2026-07-14-webapp-foundation-hardening-design.md`.

---

## Task 1: Document cú pháp bind của UI kit (Phase 1)

**Files:**
- Modify: `src/Web/Exam.WebApp/Components/UI/AppTextField.razor`
- Modify: `src/Web/Exam.WebApp/Components/UI/AppNumericField.razor`
- Modify: `src/Web/Exam.WebApp/Components/UI/AppSelect.razor`
- Modify: `src/Web/Exam.WebApp/Components/UI/AppDateField.razor`

**Interfaces:** Không đổi bất kỳ signature nào — chỉ thêm comment. Không ảnh hưởng task nào khác.

**Bối cảnh:** `AppTextField.Value` có kiểu `string?` — Razor coi giá trị gán trực tiếp cho tham số kiểu `string` (`Value="field"`) là literal string, không tự parse thành biến C#, phải viết `Value="@field"`. Lỗi này đã xảy ra thật 3 lần trong đợt migration trước (`AuditLog.razor`, `Questions.razor`, `Users.razor`). Ngược lại, `AppNumericField<TValue>`/`AppSelect<TValue>` (generic) và `AppDateField.Value` (`DateTime?`, không phải string) luôn được Razor parse như biểu thức C# dù có hay không có `@` — an toàn, nhưng người mới dễ nhầm lẫn nếu không có gì giải thích sự khác biệt.

- [ ] **Step 1: Thêm cảnh báo vào `AppTextField.razor`**

Trong `src/Web/Exam.WebApp/Components/UI/AppTextField.razor`, tìm dòng:

```csharp
    [Parameter] public string? Value { get; set; }
```

Thay bằng:

```csharp
    // Value là kiểu string - Razor coi giá trị gán trực tiếp (Value="field") là literal string,
    // KHÔNG tự parse thành biến C#. Luôn bind bằng "@field" (vd: Value="@keyword"), không phải "field".
    // Đã xảy ra thật 3 lần (AuditLog.razor, Questions.razor, Users.razor) khi thiếu dấu @.
    [Parameter] public string? Value { get; set; }
```

- [ ] **Step 2: Thêm ghi chú đối chiếu vào `AppNumericField.razor`**

Trong `src/Web/Exam.WebApp/Components/UI/AppNumericField.razor`, tìm dòng:

```csharp
    [Parameter] public TValue? Value { get; set; }
```

Thay bằng:

```csharp
    // Value ở đây là kiểu generic (TValue), không phải string, nên Razor luôn parse giá trị gán là
    // biểu thức C# dù có hay không có dấu "@" - khác với AppTextField.Value (string?), nơi thiếu "@"
    // sẽ bị hiểu nhầm thành literal string. Xem AppTextField.razor để biết chi tiết.
    [Parameter] public TValue? Value { get; set; }
```

- [ ] **Step 3: Thêm ghi chú đối chiếu vào `AppSelect.razor`**

Trong `src/Web/Exam.WebApp/Components/UI/AppSelect.razor`, tìm dòng:

```csharp
    [Parameter] public TValue? Value { get; set; }
```

Thay bằng:

```csharp
    // Value ở đây là kiểu generic (TValue), không phải string, nên Razor luôn parse giá trị gán là
    // biểu thức C# dù có hay không có dấu "@" - khác với AppTextField.Value (string?), nơi thiếu "@"
    // sẽ bị hiểu nhầm thành literal string. Xem AppTextField.razor để biết chi tiết.
    [Parameter] public TValue? Value { get; set; }
```

- [ ] **Step 4: Thêm ghi chú đối chiếu vào `AppDateField.razor`**

Trong `src/Web/Exam.WebApp/Components/UI/AppDateField.razor`, tìm dòng:

```csharp
    [Parameter] public DateTime? Value { get; set; }
```

Thay bằng:

```csharp
    // Value ở đây là kiểu DateTime?, không phải string, nên Razor luôn parse giá trị gán là biểu thức
    // C# dù có hay không có dấu "@" - khác với AppTextField.Value (string?), nơi thiếu "@" sẽ bị hiểu
    // nhầm thành literal string. Xem AppTextField.razor để biết chi tiết.
    [Parameter] public DateTime? Value { get; set; }
```

- [ ] **Step 5: Build để xác nhận không có lỗi biên dịch**

Run: `dotnet build src/Web/Exam.WebApp/Exam.WebApp.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add src/Web/Exam.WebApp/Components/UI/AppTextField.razor src/Web/Exam.WebApp/Components/UI/AppNumericField.razor src/Web/Exam.WebApp/Components/UI/AppSelect.razor src/Web/Exam.WebApp/Components/UI/AppDateField.razor
git commit -m "docs: giải thích cú pháp bind đúng ngay trong UI kit để tránh lặp lại lỗi thiếu @"
```

---

## Task 2: Gộp `Truncate()` vào `TextUtils` dùng chung (Phase 2)

**Files:**
- Create: `src/Web/Exam.WebApp/Extensions/TextUtils.cs`
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/Exams.razor.cs`
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/Exams.razor`
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/ImportQuestionsDialog.razor.cs`
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/ImportQuestionsDialog.razor`
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor.cs`
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor`

**Interfaces:**
- Produces: `Exam.WebApp.Extensions.TextUtils.Truncate(string content, int maxLength) : string` — namespace `Exam.WebApp.Extensions` đã được import toàn cục qua `global using Exam.WebApp.Extensions;` trong `src/Web/Exam.WebApp/GlobalUsings.cs`, nên gọi `TextUtils.Truncate(...)` ở bất kỳ đâu trong `Exam.WebApp` không cần thêm `using`.

- [ ] **Step 1: Tạo `TextUtils.cs`**

Tạo file `src/Web/Exam.WebApp/Extensions/TextUtils.cs`:

```csharp
namespace Exam.WebApp.Extensions;

public static class TextUtils
{
    public static string Truncate(string content, int maxLength) =>
        content.Length <= maxLength ? content : content[..maxLength] + "…";
}
```

- [ ] **Step 2: Xoá `Truncate` viết tay trong `Exams.razor.cs`, cập nhật call site trong `Exams.razor`**

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/Exams.razor.cs`, xoá dòng:

```csharp
    private static string Truncate(string content) => content.Length <= 100 ? content : content[..100] + "…";
```

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/Exams.razor`, tìm dòng (trong khối liệt kê câu hỏi của môn học):

```razor
                                            @Truncate(question.Content)
```

Thay bằng:

```razor
                                            @TextUtils.Truncate(question.Content, 100)
```

- [ ] **Step 3: Xoá `Truncate` viết tay trong `ImportQuestionsDialog.razor.cs`, cập nhật 2 call site trong `ImportQuestionsDialog.razor`**

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/ImportQuestionsDialog.razor.cs`, xoá dòng:

```csharp
    private static string Truncate(string content) => content.Length <= 90 ? content : content[..90] + "…";
```

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/ImportQuestionsDialog.razor`, tìm dòng:

```razor
                            <td>@Truncate(row.Content)</td>
```

Thay bằng:

```razor
                            <td>@TextUtils.Truncate(row.Content, 90)</td>
```

Tìm tiếp dòng:

```razor
                            <td>@Truncate(error.ContentExcerpt)</td>
```

Thay bằng:

```razor
                            <td>@TextUtils.Truncate(error.ContentExcerpt, 90)</td>
```

- [ ] **Step 4: Xoá `Truncate` viết tay trong `Questions.razor.cs`, cập nhật 2 call site (1 trong `.razor.cs`, 1 trong `.razor`)**

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor.cs`, xoá dòng:

```csharp
    private static string Truncate(string content) => content.Length <= 80 ? content : content[..80] + "…";
```

Trong cùng file, tìm dòng (trong `DeleteAsync`):

```csharp
        "Xác nhận xoá", $"Xoá câu hỏi '{Truncate(question.Content)}'?",
```

Thay bằng:

```csharp
        "Xác nhận xoá", $"Xoá câu hỏi '{TextUtils.Truncate(question.Content, 80)}'?",
```

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor`, tìm dòng:

```razor
                    <td>@Truncate(question.Content)</td>
```

Thay bằng:

```razor
                    <td>@TextUtils.Truncate(question.Content, 80)</td>
```

- [ ] **Step 5: Build để xác nhận không còn tham chiếu tới `Truncate` cũ**

Run: `dotnet build src/Web/Exam.WebApp/Exam.WebApp.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add src/Web/Exam.WebApp/Extensions/TextUtils.cs src/Web/Exam.WebApp/Components/Pages/Admin/Exams.razor.cs src/Web/Exam.WebApp/Components/Pages/Admin/Exams.razor src/Web/Exam.WebApp/Components/Pages/Admin/ImportQuestionsDialog.razor.cs src/Web/Exam.WebApp/Components/Pages/Admin/ImportQuestionsDialog.razor src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor.cs src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor
git commit -m "refactor: gộp 3 bản Truncate() trùng lặp thành TextUtils.Truncate() dùng chung"
```

---

## Task 3: Tạo `IApiErrorHandler`/`ApiErrorHandler` và đăng ký DI (Phase 3, phần 1/3)

**Files:**
- Create: `src/Web/Exam.WebApp/Services/ApiErrorHandler.cs`
- Modify: `src/Web/Exam.WebApp/Program.cs`

**Interfaces:**
- Consumes: `IAppToastService.Add(string message, AppSeverity severity = AppSeverity.Normal)` (đã có, `src/Web/Exam.WebApp/Services/AppToastService.cs`); `ExamApiException` (đã có, `src/Web/Exam.WebApp/Services/ExamApiClient.cs:254`, cùng namespace `Exam.WebApp.Services` nên không cần `using` thêm); `AppTableData<TItem>` (đã có, namespace `Exam.WebApp.Components.UI`, `src/Web/Exam.WebApp/Components/UI/AppTableState.cs`).
- Produces: `Exam.WebApp.Services.IApiErrorHandler` với 2 method — `Task ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null)` và `Task<AppTableData<TItem>> LoadAppTableDataAsync<TItem>(Func<Task<AppTableData<TItem>>> load, string errorPrefix)` — Task 4 và Task 5 sẽ inject và gọi service này.

- [ ] **Step 1: Tạo `ApiErrorHandler.cs`**

Tạo file `src/Web/Exam.WebApp/Services/ApiErrorHandler.cs`:

```csharp
using Exam.WebApp.Components.UI;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Exam.WebApp.Services;

public interface IApiErrorHandler
{
    Task ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null);

    Task<AppTableData<TItem>> LoadAppTableDataAsync<TItem>(Func<Task<AppTableData<TItem>>> load, string errorPrefix);
}

public sealed class ApiErrorHandler(IAppToastService toast) : IApiErrorHandler
{
    private const string ConnectivityErrorMessage = "Không kết nối được máy chủ, vui lòng thử lại sau.";

    public async Task ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null)
    {
        try
        {
            await action();
            if (successMessage != null)
                toast.Add(successMessage, AppSeverity.Success);
        }
        catch (ExamApiException ex)
        {
            toast.Add($"{errorPrefix}: {ex.Message}", AppSeverity.Error);
        }
        catch (Exception ex) when (IsConnectivityFailure(ex))
        {
            toast.Add($"{errorPrefix}: {ConnectivityErrorMessage}", AppSeverity.Error);
        }
    }

    public async Task<AppTableData<TItem>> LoadAppTableDataAsync<TItem>(Func<Task<AppTableData<TItem>>> load, string errorPrefix)
    {
        try
        {
            return await load();
        }
        catch (ExamApiException ex)
        {
            toast.Add($"{errorPrefix}: {ex.Message}", AppSeverity.Error);
            return new AppTableData<TItem> { Items = [], TotalItems = 0 };
        }
        catch (Exception ex) when (IsConnectivityFailure(ex))
        {
            toast.Add($"{errorPrefix}: {ConnectivityErrorMessage}", AppSeverity.Error);
            return new AppTableData<TItem> { Items = [], TotalItems = 0 };
        }
    }

    private static bool IsConnectivityFailure(Exception ex) =>
        ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException;
}
```

- [ ] **Step 2: Đăng ký DI trong `Program.cs`**

Trong `src/Web/Exam.WebApp/Program.cs`, tìm 2 dòng:

```csharp
builder.Services.AddScoped<IAppToastService, AppToastService>();
builder.Services.AddScoped<IAppDialogService, AppDialogService>();
```

Thay bằng:

```csharp
builder.Services.AddScoped<IAppToastService, AppToastService>();
builder.Services.AddScoped<IAppDialogService, AppDialogService>();
builder.Services.AddScoped<IApiErrorHandler, ApiErrorHandler>();
```

- [ ] **Step 3: Build để xác nhận biên dịch sạch**

Run: `dotnet build src/Web/Exam.WebApp/Exam.WebApp.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)` (service này chưa được dùng ở đâu nên chưa có thay đổi hành vi runtime).

- [ ] **Step 4: Commit**

```bash
git add src/Web/Exam.WebApp/Services/ApiErrorHandler.cs src/Web/Exam.WebApp/Program.cs
git commit -m "feat: thêm IApiErrorHandler dùng chung, tách từ PageBase.ExecuteAsync"
```

---

## Task 4: `PageBase` chuyển sang gọi `IApiErrorHandler` (Phase 3, phần 2/3)

**Files:**
- Modify: `src/Web/Exam.WebApp/Components/PageBase.cs`

**Interfaces:**
- Consumes: `IApiErrorHandler` (Task 3).
- Produces: `PageBase.ExecuteAsync`/`PageBase.LoadAppTableDataAsync` **giữ nguyên 100% signature công khai hiện có** — mọi trang kế thừa `PageBase` (bao gồm `TakeExam.razor.cs`, dùng ở 4 chỗ: `LoadAsync`, nộp bài tự động khi hết giờ, `SaveAnswerAsync`, và 1 chỗ khác) không cần sửa gì.

- [ ] **Step 1: Thay nội dung `PageBase.cs`**

Thay toàn bộ nội dung `src/Web/Exam.WebApp/Components/PageBase.cs` (hiện có 55 dòng, gồm cả `IsConnectivityFailure` và các `using Polly.*`) bằng:

```csharp
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
```

Lưu ý: `IsConnectivityFailure` và các `using Polly.CircuitBreaker;`/`using Polly.Timeout;` bị xoá khỏi file này — logic đã chuyển hẳn vào `ApiErrorHandler` (Task 3). Đã xác nhận `IsConnectivityFailure` không được gọi từ bất kỳ file nào khác ngoài `PageBase.cs`, nên xoá an toàn.

- [ ] **Step 2: Build để xác nhận mọi trang kế thừa `PageBase` vẫn biên dịch được**

Run: `dotnet build src/Web/Exam.WebApp/Exam.WebApp.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add src/Web/Exam.WebApp/Components/PageBase.cs
git commit -m "refactor: PageBase.ExecuteAsync/LoadAppTableDataAsync gọi qua IApiErrorHandler, giữ nguyên signature"
```

---

## Task 5: `FormDialogBase` dùng chung `ExecuteAsync`, vá `ImportQuestionsDialog`, test tay luồng thi (Phase 3, phần 3/3)

**Files:**
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/FormDialogBase.cs`
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/ImportQuestionsDialog.razor.cs`

**Interfaces:**
- Consumes: `IApiErrorHandler` (Task 3).
- Produces: `FormDialogBase.ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null) : Task` — cùng chữ ký với `PageBase.ExecuteAsync`, để mọi `FormDialogBase` con (`CategoryFormDialog`, `ClassFormDialog`, `ExamFormDialog`, `ImportQuestionsDialog`, `QuestionFormDialog`) có thể dùng khi cần gọi API trực tiếp thay vì tự viết `try/catch`.

- [ ] **Step 1: Thêm `ExecuteAsync` vào `FormDialogBase`**

Thay toàn bộ nội dung `src/Web/Exam.WebApp/Components/Pages/Admin/FormDialogBase.cs` bằng:

```csharp
using Exam.WebApp.Components.UI;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages.Admin;

public abstract class FormDialogBase : ComponentBase
{
    [CascadingParameter] protected IAppDialogInstance Dialog { get; set; } = null!;
    [Inject] protected IApiErrorHandler ErrorHandler { get; set; } = null!;

    protected AppForm form = null!;

    protected Task<bool> ValidateAsync() => form.ValidateAsync();

    protected void Cancel() => Dialog.Cancel();

    protected Task ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null) =>
        ErrorHandler.ExecuteAsync(action, errorPrefix, successMessage);
}
```

- [ ] **Step 2: Refactor `ImportQuestionsDialog.razor.cs` để dùng `ExecuteAsync` thay vì `try/catch` viết tay**

Thay toàn bộ nội dung `src/Web/Exam.WebApp/Components/Pages/Admin/ImportQuestionsDialog.razor.cs` bằng:

```csharp
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ImportQuestionsDialog : FormDialogBase
{
    private const int MaxPreviewRows = 50;

    [Inject] private ExamApiClient Api { get; set; } = null!;

    private IBrowserFile? selectedFile;
    private byte[]? fileBytes;
    private ImportQuestionsResultDto? previewResult;
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
            // gọi lại lần 2 (cho bước Xác nhận) làm rớt circuit SignalR ("Cannot send data if the connection
            // is not in the 'Connected' State"). Đọc hẳn vào bộ nhớ ở đây rồi dùng lại byte[] cho cả 2 bước.
            await using var stream = selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();

            previewResult = await Api.ImportQuestionsAsync(new MemoryStream(fileBytes), selectedFile.Name, dryRun: true);
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
            var result = await Api.ImportQuestionsAsync(new MemoryStream(fileBytes), selectedFile.Name, dryRun: false);
            Dialog.Close(AppDialogResult.Ok(result));
        }, "Nhập câu hỏi thất bại");
        isBusy = false;
    }
}
```

Lưu ý: `Toast` không còn được inject trực tiếp (không còn `try/catch` tự viết cần gọi `Toast.Add`); `ExecuteAsync` giờ bắt được cả lỗi mất kết nối (`HttpRequestException`/`BrokenCircuitException`/`TimeoutRejectedException`) mà bản viết tay trước đây bỏ sót — đây là gap thật đã ghi trong spec (mục 1 phần đánh giá vấn đề). Hàm `Truncate` không còn trong file này (đã bị xoá ở Task 2), không cần xử lý lại.

- [ ] **Step 3: Build để xác nhận biên dịch sạch**

Run: `dotnet build src/Web/Exam.WebApp/Exam.WebApp.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Build và chạy lại docker-compose để kiểm tra thủ công**

Run: `docker compose up -d --build identity.server exam.api exam.webapp`
Expected: cả 3 container start thành công, `docker compose ps` cho thấy trạng thái `running`/`healthy`.

- [ ] **Step 5: Test tay luồng Import Excel (xác nhận vá đúng gap ở `ImportQuestionsDialog`)**

Vào `http://localhost:6004/admin/questions` (đăng nhập Admin hoặc Instructor), chọn 1 môn học, bấm "Import Excel", chọn 1 file `.xlsx` hợp lệ.
Expected: bảng xem trước hiển thị đúng như trước khi refactor; bấm "Xác nhận nhập" thấy toast thành công đúng nội dung như trước.

Sau đó tạm dừng `exam.api` (`docker compose stop exam.api`), thử bấm "Import Excel" lại với 1 file khác.
Expected: xuất hiện toast lỗi "Xem trước thất bại: Không kết nối được máy chủ, vui lòng thử lại sau." (không phải màn hình lỗi Blazor "An unhandled error has occurred") — đây là hành vi MỚI so với trước refactor (trước đây lỗi kết nối ở dialog này không được bắt).
Sau khi xác nhận xong, khởi động lại: `docker compose start exam.api`.

- [ ] **Step 6: Test tay đầy đủ luồng làm bài thi thật (`TakeExam`) — bắt buộc vì `PageBase` (Task 4) được `TakeExam.razor.cs` dùng ở 4 chỗ**

Đăng nhập bằng 1 tài khoản học viên, vào `/exams`, chọn 1 đề thi đang mở, bấm "Bắt đầu làm bài".
Expected: vào trang làm bài (`/take-exam/{attemptId}`), câu hỏi hiển thị đầy đủ.

Trả lời vài câu, chờ vài giây.
Expected: câu trả lời được lưu tự động (không có toast lỗi xuất hiện) — xác nhận qua việc tải lại trang, câu trả lời đã chọn vẫn còn.

Chủ động bấm "Nộp bài".
Expected: chuyển tới trang kết quả (`ExamResultPage`), hiển thị điểm số đúng.

Lặp lại 1 lần nữa với 1 đề thi khác, lần này tạm dừng `exam.api` (`docker compose stop exam.api`) ngay sau khi vào trang làm bài, thử trả lời 1 câu.
Expected: toast lỗi mất kết nối hiện ra đúng như hành vi trước khi refactor (không có gì thay đổi ở đây, vì `PageBase.ExecuteAsync` giữ nguyên signature và hành vi). Khởi động lại `exam.api` sau khi test xong: `docker compose start exam.api`.

- [ ] **Step 7: Commit**

```bash
git add src/Web/Exam.WebApp/Components/Pages/Admin/FormDialogBase.cs src/Web/Exam.WebApp/Components/Pages/Admin/ImportQuestionsDialog.razor.cs
git commit -m "refactor: FormDialogBase dùng chung IApiErrorHandler.ExecuteAsync, vá ImportQuestionsDialog thiếu bắt lỗi mất kết nối"
```

---

## Task 6: Thêm helper `CanManage` dùng chung vào `AdminPageBase` (Phase 4, phần 1/3)

**Files:**
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/AdminPageBase.cs`

**Interfaces:**
- Consumes: `UserDto` và `UserRole` enum (cả hai đều namespace `Exam.Contracts`, đã import toàn cục qua `global using Exam.Contracts;` trong `GlobalUsings.cs`).
- Produces: `AdminPageBase.CanManage(UserDto? currentUser, string ownerUserId) : bool` — Task 7 (`Questions`) và Task 8 (`Classes`) sẽ gọi trực tiếp (kế thừa `AdminPageBase` nên gọi không cần tiền tố).

- [ ] **Step 1: Thêm method `CanManage`**

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/AdminPageBase.cs`, tìm dòng:

```csharp
public abstract class AdminPageBase : PageBase
{
    [Inject] protected IAppDialogService DialogService { get; set; } = null!;
```

Thay bằng:

```csharp
public abstract class AdminPageBase : PageBase
{
    [Inject] protected IAppDialogService DialogService { get; set; } = null!;

    // Khớp đúng OwnershipGuard.EnsureOwnerOrAdmin ở backend (Admin luôn được, Instructor chỉ được với
    // tài nguyên do chính mình tạo) - tính trước ở UI để ẩn/khoá các hành động chắc chắn sẽ bị 403 thay
    // vì để người dùng bấm rồi mới thấy toast lỗi. Dùng chung cho Exams/Questions/Classes.
    protected static bool CanManage(UserDto? currentUser, string ownerUserId) =>
        currentUser != null && (currentUser.Role == UserRole.Admin || currentUser.ExternalId == ownerUserId);
```

- [ ] **Step 2: Build để xác nhận biên dịch sạch**

Run: `dotnet build src/Web/Exam.WebApp/Exam.WebApp.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)` (method chưa được gọi ở đâu nên chưa đổi hành vi runtime).

- [ ] **Step 3: Commit**

```bash
git add src/Web/Exam.WebApp/Components/Pages/Admin/AdminPageBase.cs
git commit -m "feat: thêm AdminPageBase.CanManage() dùng chung cho pattern ownership-mirroring"
```

---

## Task 7: Vá ownership-mirroring cho `Questions.razor`/`Questions.razor.cs` (Phase 4, phần 2/3)

**Files:**
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor.cs`
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor`

**Interfaces:**
- Consumes: `AdminPageBase.CanManage(UserDto?, string) : bool` (Task 6); `ExamApiClient.GetMeAsync() : Task<UserDto>` (đã có, `src/Web/Exam.WebApp/Services/ExamApiClient.cs:18-19`); `QuestionDto.OwnerUserId` (đã có, `src/Services/Exam/Exam.Contracts/QuestionDto.cs:13`).

- [ ] **Step 1: Thêm `currentUser`, load ở `OnInitializedAsync`**

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor.cs`, tìm khối:

```csharp
    private IReadOnlyCollection<CategoryDto>? categories;
    private AppTable<QuestionDto>? table;
```

Thay bằng:

```csharp
    private IReadOnlyCollection<CategoryDto>? categories;
    private UserDto? currentUser;
    private AppTable<QuestionDto>? table;
```

Tìm tiếp:

```csharp
    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () => categories = await Api.GetCategoriesAsync(), "Không tải được danh sách môn học");
```

Thay bằng:

```csharp
    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () =>
        {
            currentUser = await Api.GetMeAsync();
            categories = await Api.GetCategoriesAsync();
        }, "Không tải được danh sách môn học");
```

- [ ] **Step 2: Áp dụng `CanManage` vào các nút thao tác trên từng dòng trong `Questions.razor`**

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor`, tìm khối `RowTemplate`:

```razor
            <RowTemplate Context="question">
                <tr class="@(IsSelected(question) ? "selected" : "")">
                    <td><AppCb Value="IsSelected(question)" OnClick="() => ToggleSelect(question)" /></td>
                    <td>@TextUtils.Truncate(question.Content, 80)</td>
                    <td>@QuestionTypeLabel(question.QuestionType)</td>
                    <td>@question.Level.ToLabel()</td>
                    <td>@question.Points</td>
                    <td>@question.Answers.Count(a => a.IsCorrect)/@question.Answers.Count đúng</td>
                    <td>
                        <AppButton OnClick="() => OpenEditDialog(question)">✎</AppButton>
                        <AppButton Variant="AppButtonVariant.Danger" OnClick="() => DeleteAsync(question)">🗑</AppButton>
                    </td>
                </tr>
            </RowTemplate>
```

Thay bằng:

```razor
            <RowTemplate Context="question">
                <tr class="@(IsSelected(question) ? "selected" : "")">
                    <td>
                        <AppCb Value="IsSelected(question)" OnClick="() => ToggleSelect(question)"
                               Disabled="!CanManage(currentUser, question.OwnerUserId)"
                               Title="@(CanManage(currentUser, question.OwnerUserId) ? null : "Không phải câu hỏi của bạn")" />
                    </td>
                    <td>@TextUtils.Truncate(question.Content, 80)</td>
                    <td>@QuestionTypeLabel(question.QuestionType)</td>
                    <td>@question.Level.ToLabel()</td>
                    <td>@question.Points</td>
                    <td>@question.Answers.Count(a => a.IsCorrect)/@question.Answers.Count đúng</td>
                    <td>
                        <AppButton Disabled="!CanManage(currentUser, question.OwnerUserId)" OnClick="() => OpenEditDialog(question)">✎</AppButton>
                        <AppButton Variant="AppButtonVariant.Danger" Disabled="!CanManage(currentUser, question.OwnerUserId)" OnClick="() => DeleteAsync(question)">🗑</AppButton>
                    </td>
                </tr>
            </RowTemplate>
```

Lưu ý thiết kế: khoá checkbox chọn ngay từ đầu (thay vì cho chọn rồi mới chặn nút hàng loạt) đảm bảo `selectedIds` không bao giờ chứa câu hỏi không thuộc quyền quản lý — nên `BulkDeleteAsync`/`ConfirmMoveAsync` trong `Questions.razor.cs` không cần sửa thêm gì, tự động an toàn.

- [ ] **Step 3: Build**

Run: `dotnet build src/Web/Exam.WebApp/Exam.WebApp.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Test tay bằng tài khoản Instructor không sở hữu câu hỏi**

Chạy `docker compose up -d --build exam.webapp` (rebuild để áp dụng thay đổi). Đăng nhập bằng 1 tài khoản Instructor KHÔNG phải người tạo phần lớn câu hỏi trong 1 môn học bất kỳ (hoặc tạo 1 câu hỏi mới bằng tài khoản Admin để có dữ liệu đối chứng), vào `/admin/questions`, chọn môn học đó.
Expected: những câu hỏi không do tài khoản đang đăng nhập tạo có checkbox, nút ✎ và 🗑 bị mờ/khoá (không bấm được); những câu hỏi do chính tài khoản này tạo (nếu có) vẫn thao tác được bình thường. Đăng nhập lại bằng Admin, xác nhận Admin luôn thao tác được với mọi câu hỏi.

- [ ] **Step 5: Commit**

```bash
git add src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor.cs src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor
git commit -m "fix: Questions.razor ẩn/khoá thao tác Sửa/Xoá/chọn hàng loạt với câu hỏi không thuộc quyền quản lý"
```

---

## Task 8: Vá ownership-mirroring cho `Classes.razor`/`Classes.razor.cs` (Phase 4, phần 3/3)

**Files:**
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor.cs`
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor`

**Interfaces:**
- Consumes: `AdminPageBase.CanManage(UserDto?, string) : bool` (Task 6); `ExamApiClient.GetMeAsync() : Task<UserDto>` (đã có); `ClassRoomDto.OwnerUserId`/`ClassRoomDetailDto.OwnerUserId` (đã có, `src/Services/Exam/Exam.Contracts/ClassRoomDto.cs:3,11`).

- [ ] **Step 1: Thêm `currentUser`, load cùng danh sách lớp ở `OnInitializedAsync`**

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor.cs`, tìm khối:

```csharp
    private IReadOnlyCollection<ClassRoomDto>? classes;
    private ClassRoomDetailDto? selected;
    private string renameValue = "";

    protected override async Task OnInitializedAsync() => await LoadListAsync();
```

Thay bằng:

```csharp
    private IReadOnlyCollection<ClassRoomDto>? classes;
    private UserDto? currentUser;
    private ClassRoomDetailDto? selected;
    private string renameValue = "";

    protected override async Task OnInitializedAsync() => await ExecuteAsync(async () =>
    {
        currentUser = await Api.GetMeAsync();
        classes = await Api.GetClassesAsync();
    }, "Không tải được danh sách lớp");
```

`LoadListAsync()` (dùng lại ở `OpenCreateDialog`/`DeleteAsync`/`RenameAsync` để tải lại danh sách sau khi thao tác) giữ nguyên không đổi — không cần tải lại `currentUser` mỗi lần reload danh sách.

- [ ] **Step 2: Áp dụng `CanManage` vào khu vực chi tiết lớp trong `Classes.razor`**

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor`, tìm khối (nhánh `else` khi đã chọn 1 lớp):

```razor
        else
        {
            <h2 style="font-size:18px;margin:0 0 14px;">@selected.Name</h2>
            <div class="grid2">
                <AppCard>
                    <h3>Thông tin chung</h3>
                    <AppTextField Label="Tên lớp" @bind-Value="renameValue" />
                    <AppButton Variant="AppButtonVariant.Primary" OnClick="RenameAsync">Lưu tên lớp</AppButton>
                    <hr style="border:none;border-top:1px solid var(--line);margin:16px 0;" />
                    <div style="font-size:12px;color:var(--ink-soft);">Mã lớp (học viên nhập mã này để tham gia):</div>
                    <div class="join-code-display">@selected.JoinCode</div>
                    <AppButton OnClick="RegenerateCodeAsync">Tạo mã mới</AppButton>
                </AppCard>
                <AppCard>
                    <h3>Thành viên (@selected.Members.Count)</h3>
                    @if (selected.Members.Count == 0)
                    {
                        <div class="empty-state">Chưa có học viên nào tham gia lớp này.</div>
                    }
                    else
                    {
                        @foreach (var member in selected.Members)
                        {
                            <div class="member-row">
                                <span>@member.FullName (@member.Email)</span>
                                <AppButton Variant="AppButtonVariant.Danger" Class="p-2" OnClick="() => RemoveMemberAsync(member)">Xoá</AppButton>
                            </div>
                        }
                    }
                </AppCard>
            </div>
            <div style="margin-top:14px;">
                <AppButton Variant="AppButtonVariant.Danger" OnClick="() => DeleteAsync(classes!.First(c => c.Id == selected.Id))">Xoá lớp học</AppButton>
            </div>
        }
```

Thay bằng:

```razor
        else
        {
            <h2 style="font-size:18px;margin:0 0 14px;">@selected.Name</h2>
            @if (!CanManage(currentUser, selected.OwnerUserId))
            {
                <AppAlert Severity="AppAlertSeverity.Warning">Bạn không phải người tạo lớp học này nên chỉ có thể xem, không thể chỉnh sửa.</AppAlert>
            }
            <div class="grid2">
                <AppCard>
                    <h3>Thông tin chung</h3>
                    <AppTextField Label="Tên lớp" @bind-Value="renameValue" Disabled="!CanManage(currentUser, selected.OwnerUserId)" />
                    <AppButton Variant="AppButtonVariant.Primary" Disabled="!CanManage(currentUser, selected.OwnerUserId)" OnClick="RenameAsync">Lưu tên lớp</AppButton>
                    <hr style="border:none;border-top:1px solid var(--line);margin:16px 0;" />
                    <div style="font-size:12px;color:var(--ink-soft);">Mã lớp (học viên nhập mã này để tham gia):</div>
                    <div class="join-code-display">@selected.JoinCode</div>
                    <AppButton Disabled="!CanManage(currentUser, selected.OwnerUserId)" OnClick="RegenerateCodeAsync">Tạo mã mới</AppButton>
                </AppCard>
                <AppCard>
                    <h3>Thành viên (@selected.Members.Count)</h3>
                    @if (selected.Members.Count == 0)
                    {
                        <div class="empty-state">Chưa có học viên nào tham gia lớp này.</div>
                    }
                    else
                    {
                        @foreach (var member in selected.Members)
                        {
                            <div class="member-row">
                                <span>@member.FullName (@member.Email)</span>
                                <AppButton Variant="AppButtonVariant.Danger" Class="p-2" Disabled="!CanManage(currentUser, selected.OwnerUserId)" OnClick="() => RemoveMemberAsync(member)">Xoá</AppButton>
                            </div>
                        }
                    }
                </AppCard>
            </div>
            <div style="margin-top:14px;">
                <AppButton Variant="AppButtonVariant.Danger" Disabled="!CanManage(currentUser, selected.OwnerUserId)" OnClick="() => DeleteAsync(classes!.First(c => c.Id == selected.Id))">Xoá lớp học</AppButton>
            </div>
        }
```

- [ ] **Step 3: Build**

Run: `dotnet build src/Web/Exam.WebApp/Exam.WebApp.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Test tay bằng tài khoản Instructor không sở hữu lớp**

Chạy `docker compose up -d --build exam.webapp`. Đăng nhập bằng 1 tài khoản Instructor KHÔNG phải người tạo 1 lớp học có sẵn (hoặc tạo 1 lớp mới bằng tài khoản khác để có dữ liệu đối chứng), vào `/admin/classes`, chọn lớp đó.
Expected: thấy `AppAlert` cảnh báo màu vàng; ô nhập tên lớp, nút "Lưu tên lớp", "Tạo mã mới", "Xoá" (từng thành viên), "Xoá lớp học" đều bị mờ/khoá. Chọn 1 lớp do chính tài khoản này tạo (nếu có) — mọi thao tác hoạt động bình thường, không có cảnh báo. Đăng nhập lại bằng Admin, xác nhận Admin luôn thao tác được với mọi lớp.

- [ ] **Step 5: Commit**

```bash
git add src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor.cs src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor
git commit -m "fix: Classes.razor ẩn/khoá thao tác sửa/xoá với lớp học không thuộc quyền quản lý"
```

---

## Task 9: Xác minh toàn bộ 4 phase (tổng kết)

**Files:** không tạo/sửa file — chỉ xác minh.

- [ ] **Step 1: Build toàn bộ solution**

Run: `dotnet build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)` cho toàn bộ solution (không riêng `Exam.WebApp`).

- [ ] **Step 2: Rebuild và khởi động lại toàn bộ stack**

Run: `docker compose up -d --build identity.server exam.api exam.webapp`
Expected: 3 container start thành công.

- [ ] **Step 3: Rà lại toàn bộ Admin — không có lỗi console/UI vỡ**

Đăng nhập Admin, lần lượt vào: `/admin/dashboard`, `/admin/categories`, `/admin/audit-log`, `/admin/permissions`, `/admin/users`, `/admin/classes`, `/admin/questions`, `/admin/exams`.
Expected: mọi trang render đúng như trước (không có phần tử vỡ layout), không có lỗi nào xuất hiện trong console trình duyệt (F12 → Console), không có toast lỗi bất ngờ nào.

- [ ] **Step 4: Xác nhận lại 2 gap đã nêu trong spec đã được vá**

4a. Vào `/admin/questions`, thử thêm/sửa/xoá 1 câu hỏi bằng tài khoản Admin — xác nhận hoạt động bình thường (không bị khoá nhầm, vì `CanManage` cho Admin luôn `true`).
4b. Vào `/admin/classes`, thử tương tự bằng Admin.
4c. Với tài khoản Instructor không sở hữu (đã test riêng ở Task 7/8) — xác nhận lại 1 lần nữa sau khi mọi phase đã gộp chung, tránh trường hợp 1 phase sau vô tình ghi đè hành vi của phase trước.

- [ ] **Step 5: Dọn dẹp**

Không có bước dọn dẹp thêm — tất cả thay đổi đã được commit theo từng task ở trên.
