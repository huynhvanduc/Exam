# Tư duy & Checklist viết Unit Test tầng Exam.Application

Tài liệu này ghi lại cách tiếp cận đã dùng khi viết unit test cho tầng Application
(CategoryAggregate, RoleAggregate, UserAggregate, ClassAggregate, QuestionAggregate...),
để giữ tính nhất quán khi viết tiếp cho ExamAggregate, ExamResultAggregate hoặc bất kỳ
aggregate/handler mới nào sau này.

## 1. Quy trình cho mỗi aggregate

1. **Đọc toàn bộ source trước khi viết test nào** — với mỗi Command/Query trong aggregate, đọc đủ:
   - `*Command.cs` / `*Query.cs` (tham số, kiểu trả về)
   - `*CommandHandler.cs` / `*QueryHandler.cs` (logic thật sự)
   - `*CommandValidator.cs` / `*QueryValidator.cs` (nếu có)
   - Domain entity liên quan (constructor, method nghiệp vụ, exception ném ra) và repository interface (chữ ký method chính xác để mock đúng)
2. Liệt kê các nhánh hành vi của handler: not-found, forbidden/ownership, domain exception,
   happy path, side-effect (audit log, ghi kèm...).
3. Viết test Handler trước (Fact, không cần CSV vì mỗi case là một tình huống khác chất, không phải
   ma trận dữ liệu).
4. Viết test Validator sau: case đơn giản (rule `NotEmpty`, độ dài field) gộp vào 1 file CSV;
   case phức tạp/biên (boundary length, business rule nhiều field phối hợp) viết `[Fact]` riêng.
5. Build + `dotnet test` để xác nhận pass trước khi báo hoàn thành, không đoán.

## 2. Cấu trúc thư mục & quy ước đặt tên

```
tests/Exam.Application.UnitTests/
  <AggregateName>/
    Commands/<CommandName>Tests.cs      // 1 file test cho cả Handler + Validator của command đó
    Queries/<QueryName>QueryHandlerTests.cs
  Common/                               // test cho helper dùng chung nhiều nơi (xem mục 4)
  TestData/                             // TẤT CẢ file CSV nằm phẳng ở đây, không chia theo thư mục con
    <CommandName>CommandValidator.csv
    <QueryName>QueryValidator.csv
  CsvTestData.cs                        // helper đọc CSV -> object?[] cho [Theory]/[MemberData]
  EntityTestExtensions.cs               // helper gán Id qua reflection (xem mục 6)
```

- Class test Handler: `<Tên>CommandHandlerTests` / `<Tên>QueryHandlerTests`.
- Class test Validator: `<Tên>CommandValidatorTests` / `<Tên>QueryValidatorTests`, đặt **cùng file**
  với test Handler tương ứng (xem `CategoryAggregate/Commands/CreateCategoryTests.cs` làm mẫu).
- `.csproj` đã cấu hình `<None Include="TestData\**\*.csv" CopyToOutputDirectory="PreserveNewest" />`
  nên chỉ cần thêm file CSV vào `TestData/`, không cần sửa `.csproj`.

## 3. Khi nào dùng CSV, khi nào dùng `[Fact]`

| Tình huống | Cách viết |
|---|---|
| Nhiều field đơn giản, mỗi field có rule `NotEmpty`/độ dài ngắn, muốn liệt kê nhiều tổ hợp | 1 file CSV, `[Theory]` + `CsvTestData.Read(...)` |
| Boundary chính xác (vd đúng 200 ký tự vs 201 ký tự) | `[Fact]` riêng — nhúng chuỗi dài vào CSV vừa khó đọc vừa dễ gõ nhầm |
| Rule phối hợp nhiều field (vd "phải có ít nhất 1 đáp án đúng", "single-selection chỉ được 1 đáp án đúng") | `[Fact]` riêng, đặt tên mô tả đúng rule |
| Danh sách/collection phức tạp (vd `Answers: IReadOnlyCollection<AnswerInput>`) | Không nhét vào CSV — dựng trực tiếp trong code, `[Fact]` |
| Handler có nhiều nhánh hành vi khác nhau về chất (not-found vs forbidden vs happy path) | Luôn `[Fact]`, không CSV — đây không phải ma trận dữ liệu mà là các kịch bản khác nhau |

Quy ước cột CSV: tên cột theo tên tham số test method, cột cuối luôn là `IsValid` (bool).
Dùng converter có sẵn trong `CsvTestData`: `Str`, `NullableStr`, `Bool`, `NullableBool`, `Int`,
`NullableInt`, `Decimal`.

## 4. Cross-cutting helpers — test một lần trong `Common/`

`OwnershipGuard.EnsureOwnerOrAdmin` và `PagedResultFactory.CreateAsync` được dùng lặp lại ở
rất nhiều handler. Test đầy đủ nhánh (Admin bypass / owner khớp / forbidden, hoặc skip-take/count)
**một lần duy nhất** trong `Common/OwnershipGuardTests.cs` và `Common/PagedResultFactoryTests.cs`.
Ở test của từng handler cụ thể, chỉ cần 1 test xác nhận handler *có gọi* guard đúng cách
(vd `Handle_NotOwnerOrAdmin_ThrowsForbiddenException`), không lặp lại toàn bộ ma trận case của guard.

## 5. Mocking

- Dùng **Moq** cho mọi repository/service interface ở tầng Application (`Mock<IXxxRepository>`).
  Domain layer (nếu có test riêng) dùng fake thủ công (`Fakes/FakeXxxRepository.cs`), không dùng Moq.
- So khớp tham số kiểu `IEnumerable<T>`/mảng: **luôn dùng `It.IsAny<IEnumerable<T>>()`**, không truyền
  literal array vào `Setup(...)` — Moq so khớp bằng `Equals` mặc định, hai mảng khác instance dù cùng
  nội dung sẽ không khớp (reference equality), khiến `Setup` không bao giờ trigger và test fail khó hiểu.
- `Times.Once` / `Times.Never` / `Times.Exactly(n)` để xác nhận side-effect (Insert/Update/Delete,
  ghi AuditLog) — không chỉ assert kết quả trả về mà còn xác nhận đúng số lần gọi repository.
- Capture giá trị ghi log bằng `.Callback<T1,T2>((a,b) => captured = a)` khi cần assert nội dung
  AuditLogEntry (action, description...), xem `UpdateRolePermissionsTests.cs` hoặc
  `ImportQuestionsTests.cs` làm mẫu.

## 6. Vấn đề đã gặp và cách xử lý

- **`PagedResult<T>.TotalCount` là `long`, không phải `int`.** `Assert.Equal(1, result.TotalCount)`
  gây lỗi kiểu — luôn dùng hậu tố `L`: `Assert.Equal(1L, result.TotalCount)`.
- **Entity dựng thuần trong bộ nhớ có `Id == null`.** `Category.Create(...)`, `ClassRoom.Create(...)`...
  không tự sinh Id (Id thật chỉ được MongoDB driver gán khi Insert thật). Điều này vô hại nếu test chỉ
  dùng entity đó làm giá trị trả về mock (so khớp null==null vẫn nhất quán). Nhưng nếu handler lấy
  `category.Id` để gán làm khóa ngoại cho một aggregate khác (vd `CreateQuestionCommandHandler` dùng
  `category.Id` làm `Question.CategoryId`, và `Question` validate `CategoryId` không được rỗng) thì
  phải gán Id giả trước bằng `EntityTestExtensions.WithId(...)`:
  ```csharp
  var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
  ```
- **Excel Import/Export**: dựng workbook bằng `ClosedXML.Excel.XLWorkbook` trực tiếp trong test
  (`BuildExcel(...)` helper cục bộ trong file test), lưu ra `MemoryStream` rồi lấy `byte[]` — không
  đính kèm file `.xlsx` mẫu vào repo. Đọc lại kết quả export cũng qua `new XLWorkbook(new MemoryStream(bytes))`.
- **Command tự khai `ISkipAutoAuditLog`** (vd `ImportQuestionsCommand` vì `FileContent` là `byte[]`
  quá nặng để log tự động): handler sẽ tự ghi `IAuditLogRepository.InsertAsync` thủ công — nhớ test
  cả nhánh "ghi log khi có thay đổi" và "không ghi log khi DryRun hoặc không có gì thay đổi".

## 7. Checklist khi viết test cho một Command Handler mới

- [ ] Not-found: entity chính (và mọi entity phụ được lookup, vd category khi tạo/sửa question) không
      tồn tại → `NotFoundException`.
- [ ] Forbidden: actor không phải chủ sở hữu và không phải Admin → `ForbiddenException`
      (nếu handler có gọi `OwnershipGuard`).
- [ ] Admin bypass: Admin thao tác được dù không phải chủ sở hữu.
- [ ] Domain exception: input hợp lệ theo validator nhưng vi phạm invariant của domain entity
      (vd `ExamDomainException`) — xác nhận KHÔNG gọi Insert/Update/Delete khi exception xảy ra.
- [ ] Happy path: field trong DTO trả về đúng field đã gửi; entity được insert/update đúng
      (`Times.Once`); trả về đúng dữ liệu mong đợi.
- [ ] Side-effect: audit log được ghi đúng nội dung khi có thay đổi thật sự; KHÔNG ghi khi
      DryRun/không đổi gì.

## 8. Checklist khi viết test cho một Query Handler mới

- [ ] Not-found: trả `null` (single) hoặc list rỗng (collection), không ném exception trừ khi
      handler có chủ đích ném (vd Forbidden khi không phải member/owner/admin).
- [ ] Quyền xem: nếu có phân quyền theo Actor (Admin xem tất cả, Instructor chỉ xem của mình,
      Student chỉ xem lớp mình tham gia...), test riêng từng vai trò.
- [ ] Paged query: verify đúng `skip`/`take` được truyền xuống repository (tính từ `Page`/`PageSize`),
      và `TotalCount` lấy từ hàm count riêng — không tự đếm lại `Items.Count`.
- [ ] Export: verify đúng header, đúng số cột, đúng nội dung dòng dữ liệu (kể cả trường hợp
      nhiều đáp án đúng phải nối chuỗi đúng định dạng, trường hợp không có dữ liệu vẫn ra
      workbook chỉ có header).

## 9. Checklist khi viết test cho Validator

- [ ] Mỗi field bắt buộc (`NotEmpty`): case rỗng → invalid, case có giá trị → valid (CSV).
- [ ] Mỗi field có `MaximumLength(n)`: case đúng `n` ký tự → valid, case `n+1` ký tự → invalid (Fact riêng).
- [ ] Mỗi rule phối hợp nhiều field (`Must(...)`): test cả 2 chiều (vi phạm → invalid, tuân thủ → valid).
- [ ] Nếu field là `byte[]`/object phức tạp không tiện đưa vào CSV: viết Fact rời cho từng case
      (valid, rỗng, thiếu field khác).
