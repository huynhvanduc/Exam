# Import/Export nhanh danh sách học viên trong lớp — Design

## Bối cảnh

Từ đợt rà soát nghiệp vụ trước: `ClassRoom` hiện chỉ có 2 cách quản lý thành viên — học viên tự nhập mã lớp (`JoinClass`), hoặc giảng viên gỡ từng người một (`RemoveMember`). Không có cách nào thêm hàng loạt. Vì hệ thống chưa có luồng tự đăng ký tài khoản mới (đã xác nhận: `UsersController` không có endpoint tạo user, `Identity.Server` không có trang Đăng ký), import ở đây nghĩa là **khớp vào các tài khoản học viên đã tồn tại sẵn theo email**, không tạo tài khoản mới.

Codebase đã có sẵn 1 pattern Import/Export Excel hoàn chỉnh cho Ngân hàng câu hỏi (`ImportQuestionsCommandHandler`/`ExportQuestionsQueryHandler`, dùng ClosedXML, có bước xem trước dry-run trước khi xác nhận, báo lỗi từng dòng) — tính năng này mirror lại đúng pattern đó cho danh sách lớp học.

## Quyết định đã chốt (qua brainstorm)

1. **Định dạng: file Excel (.xlsx)**, giống hệt cách Import câu hỏi — nhất quán UX, không tạo thêm 1 cơ chế nhập liệu khác trong cùng hệ thống.
2. **File chỉ có 1 cột: Email** — đơn giản hơn nhiều so với 8 cột của Import câu hỏi, vì mục đích chỉ là khớp vào tài khoản đã có, không cần nhập lại thông tin đã biết.
3. **Email không khớp tài khoản nào đang có** → lỗi ở dòng đó ("Không tìm thấy tài khoản với email này."), không chặn các dòng hợp lệ khác — giống hệt cơ chế báo lỗi từng dòng của Import câu hỏi (không phải tất-cả-hoặc-không-gì).
4. **Email khớp tài khoản không phải vai trò Student** (Instructor/Admin) → lỗi ở dòng đó ("Tài khoản này không phải học viên.") — vì roster lớp học chỉ nên gồm học viên.
5. **Email đã là thành viên sẵn của lớp** → không lỗi, không thêm trùng (khớp hành vi idempotent có sẵn của `ClassRoom.AddMember`), hiển thị riêng trong bước xem trước là "Đã là thành viên", không tính vào số lượng "sẽ thêm mới".
6. **Import chỉ THÊM, không thay thế toàn bộ roster** — thành viên hiện có không bị gỡ nếu không có mặt trong file mới. Muốn gỡ ai vẫn dùng nút "Xoá" từng người như hiện tại.
7. **Không cần bước thông báo/xác nhận từ phía học viên trước khi thêm** — giảng viên thêm trực tiếp được, đã xác nhận phù hợp với môi trường sử dụng thực tế (đã có danh sách lớp cố định từ trước).
8. **Export**: xuất file Excel gồm Họ tên + Email của toàn bộ thành viên hiện tại của lớp — lớp chưa có thành viên vẫn xuất được (file chỉ có dòng tiêu đề).

## Thiết kế chi tiết

### 1. Domain (`Exam.Domain/AggregateModels/ClassAggregate`)

Không cần đổi gì ở `ClassRoom.cs` — `AddMember(userId)` đã idempotent (no-op nếu đã là thành viên), đã chặn owner tự thêm chính mình. Handler mới ở tầng Application sẽ gọi lại đúng method này cho từng userId hợp lệ.

### 2. Application layer — command/handler/validator mới

Theo đúng khuôn mẫu `ImportQuestionsCommandHandler`:

- **`ImportClassMembersCommand(string ClassId, byte[] FileContent, Actor Actor, bool DryRun) : IRequest<ImportClassMembersResultDto>`** — namespace `Exam.Application.ClassAggregate.Commands.ImportClassMembers`.
- **Handler**: đọc `ClassId` → `OwnershipGuard.EnsureOwnerOrAdmin`. Đọc file Excel bằng ClosedXML, mỗi dòng (bỏ header) lấy giá trị cột 1 làm email, `Trim()`. Bỏ qua dòng trống hoàn toàn. Với mỗi dòng: tra `IUserRepository` theo email (xem mục 3) → nếu không tìm thấy: lỗi "Không tìm thấy tài khoản với email này."; nếu tìm thấy nhưng `Role != UserRole.Student`: lỗi "Tài khoản này không phải học viên."; nếu tìm thấy và đã có trong `classRoom.MemberUserIds`: xếp vào nhóm "đã là thành viên"; còn lại: xếp vào nhóm "sẽ thêm mới". Nếu `DryRun == false`, gọi `classRoom.AddMember(user.ExternalId)` cho từng user ở nhóm "sẽ thêm mới" rồi `UpdateAsync`.
- **Validator**: `ClassId` không rỗng, `FileContent` không rỗng.

### 3. `IUserRepository` — thêm method tra email hàng loạt

Hiện chưa có cách tra user theo email (chỉ có theo `ExternalId`). Thêm:

```csharp
Task<IReadOnlyCollection<User>> GetByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default);
```

Implement ở `UserRepository` (Mongo) theo đúng pattern của `GetByExternalIdsAsync` đã có — filter `In` theo danh sách email đã chuẩn hoá (`ToLowerInvariant().Trim()`, so khớp không phân biệt hoa/thường vì email thường không phân biệt hoa thường trong thực tế sử dụng). Handler gọi 1 lần cho toàn bộ danh sách email trong file (không query từng dòng), rồi map kết quả vào dictionary theo email đã chuẩn hoá để tra nhanh trong vòng lặp.

### 4. Contracts (`Exam.Contracts`)

```csharp
public record ImportClassMembersResultDto(
    int TotalRows,
    int AddedCount,
    int AlreadyMemberCount,
    IReadOnlyCollection<ImportClassMemberRowError> Errors,
    IReadOnlyCollection<ImportClassMemberPreviewRow> ValidRows);

public record ImportClassMemberRowError(int RowNumber, string Email, string Message);

public record ImportClassMemberPreviewRow(int RowNumber, string Email, string FullName, bool AlreadyMember);
```

### 5. API (`ClassesController`)

- **`POST /api/classes/{id}/members/import?dryRun={bool}`** — nhận `IFormFile`, gate bởi `Permissions.Class.ManageMembers` (permission đã có sẵn, dùng lại — không thêm permission mới). Theo đúng khuôn mẫu `QuestionsController.Import` (`[RequestSizeLimit(10_000_000)]`, đọc file vào `MemoryStream`, gửi `ImportClassMembersCommand`).
- **`GET /api/classes/{id}/members/export`** — gate bởi `Permissions.Class.ManageMembers`, trả file `.xlsx` (`ExportClassMembersQueryHandler`), theo đúng khuôn mẫu `QuestionsController.Export`.

### 6. WebApp

- **`ImportClassMembersDialog.razor`** (mới, trong `Components/Pages/Admin/`) — mirror `ImportQuestionsDialog.razor`: chọn file → tự động xem trước (dry-run) → hiển thị bảng preview (cột: Dòng, Email, Họ tên, trạng thái "Sẽ thêm"/"Đã là thành viên") + danh sách lỗi từng dòng nếu có → nút "Xác nhận" gọi lại với `dryRun=false`, đóng dialog trả về `ImportClassMembersResultDto`.
- **`Classes.razor`**: thêm 2 nút "Import Excel" / "Export Excel" ngay cạnh tiêu đề "Thành viên (N)" trong card Thành viên (vị trí giống hệt toolbar của `Questions.razor`). Sau khi import xong, hiển thị toast tóm tắt (vd "Đã thêm 5 học viên, 2 đã là thành viên sẵn, 1 lỗi.") và tải lại chi tiết lớp (`selected = await Api.GetClassByIdAsync(...)`) để danh sách thành viên cập nhật ngay.
- **`ExamApiClient.cs`/`ApiRoutes.cs`**: thêm `ImportClassMembersAsync`/`ExportClassMembersAsync` theo đúng khuôn mẫu các method Import/Export câu hỏi đã có.

### 7. Excel template

1 cột duy nhất, header `"Email"`. Nút "Export Excel" đồng thời đóng vai trò file mẫu (giống Question: export ra rồi sửa lại làm file import) — không cần thêm 1 template rời riêng biệt.

## Ngoài phạm vi (deferred)

- Thay thế toàn bộ roster qua import (đã chọn phương án chỉ-thêm).
- Thông báo/email cho học viên khi được thêm vào lớp (hệ thống chưa có tính năng gửi email nói chung — nằm trong danh sách gap nghiệp vụ rộng hơn, không thuộc phạm vi đợt này).
- Gộp chung 1 helper Import/Export Excel dùng lại giữa Câu hỏi và Lớp học (cân nhắc sau nếu có use case thứ 3, tránh động vào tính năng Import câu hỏi đang chạy ổn).

## Rủi ro & Kế hoạch xác minh

- Không viết unit test (quy ước dự án). Xác minh bằng `dotnet build` + thao tác tay: import 1 file có email hợp lệ/không tồn tại/không phải học viên/đã là thành viên trộn lẫn trong cùng 1 file, xác nhận preview + kết quả cuối đúng từng nhóm; export rồi mở lại file xác nhận đúng cột.
- Rủi ro chính: so khớp email không phân biệt hoa/thường cần nhất quán ở cả tầng đọc file lẫn tầng query Mongo, tránh bỏ sót match do khác hoa/thường giữa file Excel và dữ liệu đã lưu.
