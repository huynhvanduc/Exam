# Convention cho khu vực Admin (Exam.WebApp)

Tài liệu này dành cho dev mới (fresher/junior) khi thêm/sửa trang trong `src/Web/Exam.WebApp/Components/Pages/Admin/`. Mục tiêu: đọc xong có thể thêm 1 trang admin mới đúng chuẩn mà **không cần hỏi lại** vì sao chỗ này làm thế này chỗ kia làm thế khác.

> Quy tắc vàng: **không tự viết `try/catch (ExamApiException ...)`, không tự gọi `HttpClient` trực tiếp, không tự viết switch label cho enum.** Cả 3 việc này đã có sẵn helper — xem mục tương ứng bên dưới. Nếu thấy mình đang viết những thứ đó, dừng lại và tìm helper trước.

---

## 1. Cấu trúc 1 trang admin — bắt buộc

Mỗi trang là 1 cặp file `Xxx.razor` + `Xxx.razor.cs`, không viết code C# trực tiếp trong `@code { }` của `.razor` (trừ dialog nhỏ có thể du di, xem mục 4).

**`.razor` luôn mở đầu bằng 3 dòng này** (namespace tự động nhờ file cùng thư mục):

```razor
@page "/admin/xxx"
@attribute [Authorize(Roles = "Instructor,Admin")]   @* hoặc "Admin" nếu chỉ Admin được vào *@
@inherits AdminPageBase
```

> ⚠️ **Lỗi hay gặp nhất**: quên `@inherits AdminPageBase` → Blazor tự sinh code với base class mặc định là `ComponentBase`, xung đột với `.razor.cs` khai báo `: AdminPageBase` → lỗi biên dịch `CS0263: Partial declarations ... must not specify different base classes`. Nếu gặp lỗi này, kiểm tra dòng `@inherits` trước tiên.

**`.razor.cs` luôn kế thừa `AdminPageBase`**, không phải `ComponentBase`:

```csharp
using Exam.WebApp.Services;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Xxx : AdminPageBase
{
    // ...
}
```

`AdminPageBase` (`Components/Pages/Admin/AdminPageBase.cs`) đã tự inject sẵn `Api` (`ExamApiClient`), `Snackbar`, `DialogService` — **không cần `@inject` lại 3 cái này** trong `.razor`. Chỉ `@inject` thêm nếu trang cần thứ khác (vd `NavigationManager` như `Exams.razor`/`ExamDetail.razor`).

---

## 2. Gọi API — luôn qua `ExamApiClient` + `ApiRoutes`, không tự chế

- Không bao giờ hardcode URL dạng chuỗi (`"/api/categories"`) trong `.razor.cs` — thêm route vào `Services/ApiRoutes.cs` (đã có sẵn 1 static class con cho mỗi aggregate: `ApiRoutes.Categories`, `ApiRoutes.Questions`, ...).
- Không bao giờ tự `new HttpClient()` hay tự `_httpClient.SendAsync(...)` trong page — thêm method mới vào `Services/ExamApiClient.cs`, dùng lại `SendAsync<TResponse>`/`SendForCollectionAsync<TItem>`/`SendAsync` (không generic, cho action không trả dữ liệu) đã có sẵn ở cuối file. Mỗi method mới chỉ nên 1-2 dòng, ví dụ:

```csharp
public Task<CategoryDto> CreateCategoryAsync(CategoryRequest body, CancellationToken cancellationToken = default) =>
    SendAsync<CategoryDto>(HttpMethod.Post, ApiRoutes.Categories.Base, body, cancellationToken);
```

- Mọi lỗi HTTP không thành công tự động ném `ExamApiException` (xem `EnsureSuccessAsync` trong `ExamApiClient`) — page không cần tự kiểm tra status code.

---

## 3. Xử lý lỗi/loading — luôn qua helper của `AdminPageBase`, không tự viết `try/catch`

`AdminPageBase` có 4 helper, dùng đúng cái phù hợp thay vì viết tay:

| Helper | Dùng khi nào | Ví dụ |
|---|---|---|
| `ExecuteAsync(action, errorPrefix, successMessage?)` | Gọi 1 API mutate (create/update/toggle...), muốn Snackbar báo lỗi/thành công tự động | `await ExecuteAsync(() => Api.CreateCategoryAsync(data), "Tạo thất bại", "Đã thêm môn học.")` |
| `ConfirmAndExecuteAsync(title, message, action, errorPrefix, successMessage?)` | Hành động cần hỏi xác nhận trước (xoá, lưu trữ...) | `DeleteAsync` ở mọi trang — xem `Categories.razor.cs` |
| `ShowFormDialogAsync<TDialog, TRequest>(title, parameters, options?)` | Mở dialog tạo/sửa, nhận về `TRequest?` (null nếu user bấm Huỷ) | Xem mục 4 |
| `LoadTableDataAsync<TItem>(load, errorPrefix)` | Trong `LoadServerData` của `MudTable` phân trang (bắt buộc vì `ServerData` cần trả `TableData<T>` kể cả khi lỗi) | Xem mục 5 |

Không có case nào trong 7 trang admin hiện tại cần viết `try/catch` tay — nếu bạn thấy mình sắp viết, gần như chắc chắn có helper đã đủ dùng.

---

## 4. Dialog tạo/sửa — kế thừa `FormDialogBase`, gọi qua `ShowFormDialogAsync`

**Dialog** (`XxxFormDialog.razor` + `.razor.cs`) kế thừa `FormDialogBase` (`Components/Pages/Admin/FormDialogBase.cs`), không phải `ComponentBase`:

```razor
@inherits FormDialogBase

<MudDialog>
    <DialogContent>
        <MudForm @ref="form">
            ...
        </MudForm>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Huỷ</MudButton>
        <MudButton Color="Color.Primary" OnClick="Submit">Lưu</MudButton>
    </DialogActions>
</MudDialog>
```

```csharp
public partial class XxxFormDialog : FormDialogBase
{
    // Parameter nhận dữ liệu ban đầu để fill form — có thể là Request rỗng mặc định (tạo mới)
    // hoặc DTO nullable (null = tạo mới, có giá trị = sửa), tuỳ độ phức tạp của form. Xem 2 cách
    // làm khác nhau ở CategoryFormDialog.razor.cs (Request) và QuestionFormDialog.razor.cs (DTO?).
    [Parameter] public XxxRequest Model { get; set; } = new(...);

    private async Task Submit()
    {
        if (!await ValidateAsync())   // gọi form.ValidateAsync() + kiểm tra form.IsValid, không tự viết lại
            return;

        MudDialog.Close(DialogResult.Ok(new XxxRequest(...)));
    }
}
```

`FormDialogBase` đã có sẵn `MudDialog` (cascading), `form` (ref), `ValidateAsync()`, `Cancel()` — **không khai báo lại các field/method này** trong dialog con.

**Trang gọi dialog** qua `ShowFormDialogAsync<TDialog, TRequest>` của `AdminPageBase`, không tự `DialogService.ShowAsync` + tự đọc `dialog.Result`:

```csharp
private async Task OpenCreateDialog()
{
    var parameters = new DialogParameters<XxxFormDialog>();   // để trống = dùng giá trị mặc định của Model
    var data = await ShowFormDialogAsync<XxxFormDialog, XxxRequest>("Thêm...", parameters);
    if (data == null) return;   // user bấm Huỷ

    await ExecuteAsync(() => Api.CreateXxxAsync(data), "Tạo thất bại", "Đã thêm.");
    // gọi lại LoadAsync() hoặc table.ReloadServerData() tuỳ trang có phân trang hay không
}
```

Template tham khảo: `CategoryFormDialog.razor(.cs)` (đơn giản nhất), `QuestionFormDialog.razor(.cs)` (có validate phức tạp hơn — nhiều đáp án).

---

## 5. Danh sách có phân trang hay không? — tiêu chí quyết định

**Không phải cứ có bảng là phải phân trang.** Quyết định dựa trên: *dữ liệu này có thể phình to không giới hạn theo thời gian không?*

| Loại dữ liệu | Có thể phình to? | Cách làm |
|---|---|---|
| Category (môn học), RolePermissions | Không — số lượng cố định/nhỏ, do Admin tự quản lý | `MudTable Items="collection"`, load 1 lần bằng `Api.GetXxxAsync()` trả `IReadOnlyCollection<T>` |
| Question, Exam, User, AuditLog | Có — tăng dần theo thời gian sử dụng | `MudTable ServerData="LoadServerData"`, backend trả `PagedResult<T>` |

**Nếu KHÔNG phân trang** — copy `Categories.razor` + `Categories.razor.cs` làm mẫu:
```csharp
private IReadOnlyCollection<CategoryDto>? categories;

private Task LoadAsync() =>
    ExecuteAsync(async () => categories = await Api.GetCategoriesAsync(), "Không tải được danh sách");
```
Trong `.razor`: `@if (categories == null) { <MudProgressCircular Indeterminate="true" /> } else { <MudTable Items="categories" ...> }`.

**Nếu CÓ phân trang** — copy `AuditLog.razor` + `AuditLog.razor.cs` (đơn giản nhất, không có filter) hoặc `Questions.razor` (có thêm dropdown lọc theo category) làm mẫu:
```csharp
private MudTable<AuditLogEntryDto>? table;

private Task<TableData<AuditLogEntryDto>> LoadServerData(TableState state, CancellationToken cancellationToken) =>
    LoadTableDataAsync(async () =>
    {
        var result = await Api.GetAuditLogAsync(state.Page + 1, state.PageSize, cancellationToken);
        return new TableData<AuditLogEntryDto> { Items = result.Items, TotalItems = (int)result.TotalCount };
    }, "Không tải được nhật ký");
```
Trong `.razor`: `<MudTable T="AuditLogEntryDto" @ref="table" ServerData="LoadServerData" ...>` kèm `<PagerContent><MudTablePager /></PagerContent>`.

> `state.Page` của MudBlazor bắt đầu từ **0**, còn API bắt đầu từ **1** — luôn `state.Page + 1` khi gọi API, đừng quên +1 này (lỗi phổ biến khi thêm trang mới).

Sau khi create/edit/delete thành công ở trang có phân trang, gọi `if (table != null) await table.ReloadServerData();` thay vì tự set lại field danh sách.

---

## 6. Hiển thị label cho enum — qua `EnumDisplayExtensions`, không viết switch riêng

`Level`, `ExamStatus` đã có extension method sẵn trong `Extensions/EnumDisplayExtensions.cs`:

```razor
@context.Level.ToLabel()
@context.Status.ToLabel()
<MudChip Color="@context.Status.ToColor()">...</MudChip>
```

Nếu cần thêm enum mới cần hiển thị label tiếng Việt (vd `QuestionType`), **thêm method vào `EnumDisplayExtensions.cs`**, không viết `private static string XxxLabel(...)` riêng trong từng trang — kể cả khi lúc đó chỉ có 1 trang dùng, vì kinh nghiệm cho thấy label kiểu này luôn bị cần lại ở trang thứ 2 sau đó và bị copy-paste thay vì tái sử dụng.

---

## 8. Markup dùng chung — `Components/Shared/`, không viết lại tay

Nếu thấy mình sắp gõ lại `<MudText Typo="Typo.h4" GutterBottom="true">...</MudText>` hay `<MudSelect Label="Môn học" ...>` từ đầu — **dừng lại, dùng component có sẵn trong `Components/Shared/`** (đã đăng ký `@using Exam.WebApp.Components.Shared` ở `Components/_Imports.razor`, dùng thẳng không cần `@using` riêng):

- **`<AdminPageHeader Title="..." Subtitle="...">`** — tiêu đề h4 + subtitle tuỳ chọn + `ChildContent` (nút "Thêm...", `<CategorySelector>`, hoặc để trống). Dùng ở đầu mọi trang admin, xem `Categories.razor`/`Users.razor` để thấy 2 cách dùng (có/không có nút con).
- **`<CategorySelector Categories="..." Value="..." ValueChanged="...">`** — dropdown chọn môn học, chỉ dùng khi trang cần lọc danh sách theo category (như `Questions.razor`, `Exams.razor`).

> `ExamDetail.razor` **không** dùng `AdminPageHeader` — header của nó có nút back + chip trạng thái + nhiều nút hành động điều kiện, cấu trúc khác hẳn nên giữ nguyên viết tay. Đừng cố ép nó dùng chung.

Nút Edit/Delete trong `RowTemplate` của `MudTable` **cố tình không** tách thành component chung — 3 trang có nút này (Categories/Questions/Exams) đủ khác nhau (Exams có thêm nút Settings + điều kiện Disabled riêng) khiến 1 component chung sẽ cần nhiều tham số tuỳ chọn hơn lượng code tiết kiệm được. Cứ viết tay 2 dòng `<MudIconButton>` như các trang hiện có.

---

## 9. Checklist nhanh khi thêm 1 trang admin mới

1. [ ] `Xxx.razor` có `@page`, `@attribute [Authorize(Roles = "...")]`, `@inherits AdminPageBase`.
2. [ ] `Xxx.razor.cs` là `public partial class Xxx : AdminPageBase`.
3. [ ] Không có `@inject ExamApiClient`/`ISnackbar`/`IDialogService` thừa (đã có từ base).
4. [ ] Mọi gọi API đi qua method mới thêm vào `ExamApiClient.cs`, route định nghĩa ở `ApiRoutes.cs`.
5. [ ] Mọi mutate action bọc trong `ExecuteAsync`/`ConfirmAndExecuteAsync`, không có `try/catch` tay.
6. [ ] Đã xác định danh sách này có cần phân trang không theo tiêu chí mục 5, và làm đúng pattern tương ứng.
7. [ ] Dialog tạo/sửa (nếu có) kế thừa `FormDialogBase`, mở qua `ShowFormDialogAsync`.
8. [ ] Không viết switch label enum riêng — dùng/mở rộng `EnumDisplayExtensions`.
9. [ ] Header trang dùng `<AdminPageHeader>` (và `<CategorySelector>` nếu có lọc theo category) thay vì viết tay `<MudText Typo="Typo.h4">`.
10. [ ] Thêm link vào `MainLayout.razor` (`MudNavLink`) với đúng `AuthorizeView Roles="..."` tương ứng quyền của trang.
11. [ ] Build (`dotnet build`) sạch trước khi coi là xong — lỗi `CS0263` (mục 1) là dấu hiệu sót `@inherits`.
