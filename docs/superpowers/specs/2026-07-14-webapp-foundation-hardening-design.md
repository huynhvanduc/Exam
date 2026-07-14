# Exam.WebApp Foundation Hardening — Design (Hướng A)

## Bối cảnh

Đợt migration MudBlazor → AppUI kit vừa hoàn tất cho toàn bộ khu vực Admin của
`Exam.WebApp` (Blazor Server). Trong quá trình sửa các bug phát sinh
(`@` prefix cho tham số kiểu string, `StateHasChanged` không tự lan từ
component con lên component cha, pageSize lệch giữa client/server, UI không
phản ánh đúng ownership-check của backend), lộ ra 4 điểm bất nhất trong nền
tảng dùng chung của toàn bộ trang Admin — không phải bug đơn lẻ mà là nguyên
nhân khiến bug đơn lẻ dễ tái diễn khi có thêm người (fresher/junior) viết
thêm trang mới.

**Phạm vi:** chỉ tầng Blazor app (`src/Web/Exam.WebApp`). Các API backend
(CQRS layer) hiếm khi cần sửa và không nằm trong phạm vi đợt này, trừ khi một
phase dưới đây phát hiện chỗ cần đối chiếu logic đã có sẵn ở backend (không
đổi logic backend).

**Đội hình:** hiện chỉ 1 dev, sắp tuyển thêm fresher/junior — không có
peer-review an toàn lưới, nên nền tảng dùng chung phải đủ rõ ràng để người
mới tự làm theo mà không tự sinh ra lỗi logic hoặc code thiếu nhất quán.

**Không viết unit test** cho đợt này (quy ước đã có từ trước cho
`Exam.Domain`, áp dụng tương tự ở đây) — xác minh bằng build + so sánh trực
quan/thao tác tay với mockup HTML và luồng thực tế, đặc biệt luồng làm bài
thi (`TakeExam`) vì đây là luồng có rủi ro cao nhất khi người dùng thật đang
thi.

## Đánh giá vấn đề hiện trạng (đã xác minh trực tiếp trong code)

1. **`FormDialogBase` không kế thừa `PageBase`** (`FormDialogBase.cs:7` —
   `ComponentBase`, không phải `PageBase`), nên không có `ExecuteAsync`. Hệ
   quả thực tế: `ImportQuestionsDialog.razor.cs` phải tự inject lại `Api`
   và `Toast` (dòng 11-12) rồi tự viết `try/catch (ExamApiException ex)` tay
   ở 2 chỗ (`PreviewAsync` dòng 37-56, `ConfirmAsync` dòng 65-77) — và bản tự
   viết này **thiếu hẳn nhánh bắt lỗi mất kết nối**
   (`HttpRequestException`/`BrokenCircuitException`/`TimeoutRejectedException`)
   mà `PageBase.ExecuteAsync` (`PageBase.cs:28`) đã xử lý. Nghĩa là dialog
   này là điểm duy nhất trong Admin sẽ không hiển thị toast lỗi thân thiện
   khi mất kết nối server — nó sẽ để lỗi rơi tự do. Đây không phải rủi ro lý
   thuyết, mà là một gap thật đang tồn tại.

2. **Hàm `Truncate(string, int)` bị viết tay lặp lại 3 lần với 3 độ dài tuỳ
   tiện khác nhau**, không có lý do nghiệp vụ nào giải thích tại sao khác
   nhau:
   - `Exams.razor.cs:273` → 100 ký tự
   - `ImportQuestionsDialog.razor.cs:80` → 90 ký tự
   - `Questions.razor.cs:186` → 80 ký tự

3. **Pattern "UI phản chiếu ownership-check của backend" mới chỉ áp dụng
   cho `Exams.razor`/`Exams.razor.cs`** (`CanManageSelected`, thêm tuần
   trước để vá đúng 1 bug thật: Instructor không phải chủ sở hữu đề thi vẫn
   thấy đầy đủ nút Sửa/Xuất bản/Xoá/xem Kết quả rồi mới nhận toast lỗi 403).
   Xác nhận bằng đọc trực tiếp `Questions.razor.cs` và `Classes.razor.cs`:
   **cả hai đều không có `currentUser`, không có bất kỳ điều kiện ẩn/khoá
   nút nào dựa trên quyền sở hữu** — trong khi backend đã áp dụng
   `OwnershipGuard.EnsureOwnerOrAdmin` cho các handler tương ứng
   (`DeleteQuestionCommandHandler`, `UpdateQuestionCommandHandler`,
   `RenameClassRoomCommandHandler`, `DeleteClassRoomCommandHandler`,
   `RemoveMemberCommandHandler`, `RegenerateJoinCodeCommandHandler`, và
   nhiều handler khác cùng aggregate). Tức là **Instructor không sở hữu vẫn
   thấy đủ nút Sửa/Xoá/Đổi tên/Gỡ thành viên trên Questions và Classes ngay
   lúc này**, y hệt bug đã vá ở Exams tuần trước, chỉ là chưa ai báo vì chưa
   ai thử.

4. **Bộ UI kit (`Components/UI/App*.razor`) chưa có comment hướng dẫn cú
   pháp bind đúng** ngay tại nơi khai báo — lỗi thiếu `@` prefix (mục 1 ở
   trên, đã từng xảy ra thật ở `AuditLog.razor`/`Questions.razor`/
   `Users.razor`) chỉ được biết tới qua debug, không có gì trong code cảnh
   báo trước cho người viết trang mới.

## Xác nhận luật nghiệp vụ (đã chốt với chủ dự án)

Luật "Instructor chỉ quản lý được đề thi/câu hỏi/lớp do chính mình tạo,
Admin luôn được" là **đúng chủ đích nghiệp vụ**, giữ nguyên. Việc tổng quát
hoá UI-mirroring ở Phase 3 dưới đây là mở rộng đúng luật đã có, không phải
thay đổi luật.

## Thiết kế: 4 phase, thứ tự ưu tiên theo rủi ro thấp → cao

Từng phase độc lập, có thể dừng giữa chừng mà không để lại trạng thái dở
dang; thứ tự dưới đây được chốt sau khi cân nhắc cả góc kỹ thuật (risk kỹ
thuật) lẫn góc nghiệp vụ (ROI, rủi ro ảnh hưởng người dùng thật).

### Phase 1 — Document UI kit (rủi ro = 0)

Thêm 1 comment ngắn (không quá 2-3 dòng) ngay tại phần khai báo `[Parameter]`
liên quan tới binding của từng component chuỗi trong `Components/UI/`
(`AppTextField`, `AppNumericField`, `AppSelect`, ...), nêu rõ cú pháp bind
đúng kèm 1 ví dụ, đặc biệt nhấn cú pháp `Value="@field"` (không phải
`Value="field"`) cho tham số kiểu string. Không đổi hành vi runtime của bất
kỳ component nào.

**Giá trị:** loại bỏ hoàn toàn khả năng lặp lại đúng bug đã xảy ra 3 lần
(`AuditLog.razor`, `Questions.razor`, `Users.razor`) khi có người mới viết
trang tiếp theo, với chi phí gần bằng 0 và không cần test lại bất cứ gì.

### Phase 2 — Common hoá `Truncate()` (rủi ro = 0)

Tạo `src/Web/Exam.WebApp/Extensions/TextUtils.cs`:

```csharp
namespace Exam.WebApp.Extensions;

public static class TextUtils
{
    public static string Truncate(string content, int maxLength) =>
        content.Length <= maxLength ? content : content[..maxLength] + "…";
}
```

Xoá 3 hàm `Truncate` viết tay ở `Exams.razor.cs:273`,
`ImportQuestionsDialog.razor.cs:80`, `Questions.razor.cs:186`; thay bằng gọi
`TextUtils.Truncate(content, maxLength)` với đúng độ dài hiện có tại từng
call site (100/90/80 — **giữ nguyên độ dài từng nơi**, vì đây là quyết định
hiển thị theo layout từng trang, không phải lỗi cần thống nhất một con số).
`Extensions` namespace đã có sẵn trong `GlobalUsings.cs`
(`global using Exam.WebApp.Extensions;`) nên không cần thêm `using` ở từng
file gọi.

**Giá trị:** xoá trùng lặp, không đổi hành vi hiển thị ở bất kỳ trang nào.

### Phase 3 — Gộp chuẩn xử lý lỗi cho Page lẫn Dialog (rủi ro thấp, cần test tay kỹ)

Tách phần logic của `PageBase.ExecuteAsync`/`LoadAppTableDataAsync`/
`IsConnectivityFailure` (`PageBase.cs`) thành 1 injectable service
`IApiErrorHandler`/`ApiErrorHandler` (namespace `Exam.WebApp.Services`),
đăng ký qua DI (`builder.Services.AddScoped<IApiErrorHandler, ApiErrorHandler>()`
trong `Program.cs`). `PageBase` sửa lại để **gọi vào service này thay vì tự
triển khai**, giữ nguyên 100% method signature công khai hiện có
(`ExecuteAsync`, `LoadAppTableDataAsync`) — mọi trang kế thừa `PageBase`
(bao gồm `TakeExam.razor.cs`) không cần sửa 1 dòng nào.

`FormDialogBase` inject cùng `IApiErrorHandler` và thêm 2 method có cùng
chữ ký với `PageBase.ExecuteAsync`/không cần `LoadAppTableDataAsync` (dialog
không có bảng phân trang):

```csharp
[Inject] protected IApiErrorHandler ErrorHandler { get; set; } = null!;

protected Task ExecuteAsync(Func<Task> action, string errorPrefix, string? successMessage = null) =>
    ErrorHandler.ExecuteAsync(action, errorPrefix, successMessage);
```

Sau đó sửa `ImportQuestionsDialog.razor.cs` để dùng `ExecuteAsync` thay cho
2 khối `try/catch` viết tay hiện có (dòng 37-56 và 65-77), xoá luôn 2 field
inject trùng lặp (`Api`, `Toast` ở dòng 11-12, kế thừa sẵn từ chuẩn dùng
chung). Đây là lần đầu dialog này thực sự bắt được lỗi mất kết nối đúng
chuẩn — vá đúng gap đã nêu ở mục 1 phần đánh giá.

**Bắt buộc trước khi coi phase này hoàn tất** (vì `PageBase` được dùng bởi
`TakeExam.razor.cs` — luồng làm bài thi thật, có tính giờ, tự động nộp bài
khi hết giờ qua `ExecuteAsync` ở dòng 65):
- `dotnet build` sạch.
- Test tay đầy đủ 1 lượt làm bài thi từ đầu đến cuối: vào đề thi → trả lời
  từng câu (xác nhận `SaveAnswerAsync` vẫn lưu đúng) → để hết giờ hoặc chủ
  động nộp bài → xác nhận trang kết quả hiển thị đúng.
- Ngắt kết nối mạng giả lập giữa chừng (hoặc dừng tạm `exam.api` container)
  để xác nhận toast lỗi mất kết nối vẫn hiện đúng như trước khi đổi.

### Phase 4 — Tổng quát hoá pattern ownership-mirroring, áp dụng cho Questions và Classes (giá trị cao nhất — vá lỗ hổng thật)

Thêm vào `AdminPageBase.cs` 1 property có thể tái sử dụng:

```csharp
protected bool CanManage(UserDto? currentUser, string ownerUserId) =>
    currentUser != null && (currentUser.Role == UserRole.Admin || currentUser.ExternalId == ownerUserId);
```

**Questions.razor.cs:** thêm `currentUser` (load ở `OnInitializedAsync`
cùng `categories`, giống `Exams.razor.cs` đã làm). `QuestionDto` đã sẵn có
`OwnerUserId` (`QuestionDto.cs:13`) — không cần đổi hợp đồng API. Áp dụng
`Disabled="!CanManage(currentUser, question.OwnerUserId)"` cho nút Sửa/Xoá
của từng dòng, và ẩn checkbox chọn hàng loạt (`ToggleSelect`)/vô hiệu nút
"Chuyển câu hỏi"/"Xoá đã chọn" khi có bất kỳ câu hỏi nào trong lựa chọn
không thuộc quyền quản lý.

**Classes.razor.cs:** thêm `currentUser` tương tự. `ClassRoomDto` và
`ClassRoomDetailDto` đã sẵn có `OwnerUserId` (`ClassRoomDto.cs:3,11`) —
không cần đổi hợp đồng API. Áp dụng cho nút Đổi tên, Tạo mã mới, Gỡ thành
viên, Xoá lớp — theo đúng mẫu đã làm ở `Exams.razor`/`Exams.razor.cs`
(`AppAlert` cảnh báo khi `!CanManage(...)`, ẩn nút "Làm mới"/hành động sửa
khi không có quyền).

**Giá trị:** đây là phase duy nhất sửa một lỗ hổng UX/bảo mật-cảm-nhận thật
đang tồn tại ngay lúc này (Instructor không sở hữu vẫn thấy đủ nút thao
tác trên Questions/Classes, chỉ nhận toast lỗi 403 sau khi bấm) — không chỉ
là dọn code.

## Ngoài phạm vi đợt này (deferred, ghi nhận nhưng không làm)

- Tách `ExamApiClient` (262 dòng, ~50+ method phẳng, không có interface)
  thành nhiều client theo domain + thêm `IExamApiClient`.
- Tái cấu trúc thư mục `Components/Pages/Admin/` theo feature-folder.
- Mở rộng luật ownership để cho phép chia sẻ giữa các Instructor cùng
  môn/lớp — đã xác nhận đây KHÔNG phải mục tiêu đợt này, luật hiện tại giữ
  nguyên.

Lý do: Hướng A ưu tiên rủi ro thấp, tăng dần theo từng bước, không tái cấu
trúc thư mục ở đợt này.

## Rủi ro & Kế hoạch xác minh chung

- Không có unit test — xác minh hoàn toàn bằng `dotnet build` (0 lỗi/cảnh
  báo) + thao tác tay trực tiếp trên app đang chạy (docker compose).
- Rủi ro cao nhất tập trung ở Phase 3 (chạm hạ tầng dùng chung với
  `TakeExam`) — đã có bước test tay bắt buộc riêng ở trên.
- Phase 4 thay đổi hành vi hiển thị thật (ẩn/khoá nút) trên Questions và
  Classes cho tài khoản Instructor không sở hữu — cần thông báo trước cho
  người dùng thật (giảng viên) đang dùng hệ thống, vì đây là thay đổi hành
  vi UI có thể gây bất ngờ nếu trước đó họ "quen" bấm rồi thấy lỗi.
- Đợt này không tạo ra tính năng mới nhìn thấy được — nên thông báo trước
  với khách hàng đây là đợt củng cố nền tảng (foundation hardening), tránh
  kỳ vọng sai lệch khi không thấy tính năng mới xuất hiện trong sprint này.
