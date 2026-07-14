# Ma trận cấu trúc đề thi & thang điểm 10 — Design

## Bối cảnh

Trong đợt rà soát "chuyên gia nghiệp vụ" trên toàn hệ thống, phát hiện cách chấm điểm hiện tại **không thực sự chấm theo thang điểm 10 chuẩn học vụ Việt Nam** dù trực giác người dùng nghĩ vậy — đã xác minh trực tiếp trong code:

- `ExamResult.TotalScore`/`MaxPossibleScore` là **tổng cộng dồn** field `Points` (số nguyên gõ tay) của từng câu hỏi trong ngân hàng câu hỏi — không có công thức nào ép tổng phải ra 10. Thêm/bớt 1 câu hoặc quên sửa điểm 1 câu là thang điểm lệch ngay, không cảnh báo.
- `Level` (Dễ/Vừa/Khó) trên câu hỏi **hoàn toàn không ảnh hưởng tới điểm** — chỉ là tag lọc/hiển thị, không được đọc tới trong `ExamResult.ScoreFor`/`ExamResultGradingService`.
- Chế độ **Pool** (rút ngẫu nhiên) hiện chỉ rút theo 1 môn học + 1 tổng số lượng duy nhất (`ExamQuestionPoolService.DrawQuestionIdsAsync`), không có khái niệm cơ cấu theo độ khó/loại câu hỏi.
- Chế độ **Fixed** (giảng viên tự tick từng câu) không ràng buộc cơ cấu gì cả.
- Trừ điểm khi chọn sai (`NegativeMarkingRatio`) có thể khiến `TotalScore` âm, không có sàn chặn ở 0.

Mục tiêu: đưa vào khái niệm **ma trận cấu trúc đề thi** (giống "ma trận đề thi" giáo viên vẫn lập tay trước khi ra đề thật: bảng mức độ nhận thức × số câu), để điểm luôn ra đúng thang 0–10, và độ khó thật sự có vai trò trong việc đề thi được cấu thành từ đâu.

## Quyết định phạm vi đã chốt (qua brainstorm)

1. **Ma trận áp dụng cho MỌI đề thi, thay thế hoàn toàn** cả 2 chế độ Fixed và Pool hiện có — không giữ tương thích ngược. Xác nhận: chưa có đề thi thật nào cần giữ, dữ liệu `exams`/`examResults` hiện tại sẽ bị xoá sạch khi triển khai (xem mục "Dữ liệu cũ" bên dưới).
2. **Cách điền câu hỏi vào ma trận: hệ thống tự rút ngẫu nhiên 100% theo từng ô** (mở rộng cơ chế Pool hiện có), giảng viên không tự tick tay từng câu nữa — chế độ Fixed bị xoá hoàn toàn.
3. **Cách chia điểm: chia đều toàn bộ** — không phân biệt trọng số theo độ khó. `TotalScore = (Số câu đúng / Tổng số câu) × 10`, làm tròn 2 chữ số thập phân.
4. **Bỏ hẳn trừ điểm (negative marking)** trong mô hình mới — trả lời sai hoặc bỏ trống đều = 0 điểm cho câu đó, không âm, nên không cần sàn chặn riêng.
5. **Field `Points` trên câu hỏi (ngân hàng câu hỏi) bị xoá hoàn toàn** — không còn tác dụng gì trong mô hình chia đều.
6. **Điểm đạt tối thiểu** (`MinimumPassingScore`) đổi từ số nguyên tự do sang giới hạn khoảng **0–10, cho phép số thập phân** (vd 5.0, 6.5) để khớp thang điểm mới.
7. **Kiến trúc: mở rộng trực tiếp `Exam`/`ExamQuestionPoolService` hiện có** (không tách aggregate "Ma trận đề thi" riêng — over-engineering so với nhu cầu hiện tại, mỗi đề có ma trận riêng, không cần dùng chung giữa nhiều đề).

## Thiết kế chi tiết

### 1. `Exam` (domain entity, `Exam.Domain/AggregateModels/ExamAggregate/Exam.cs`)

**Xoá:**
- `QuestionSelectionMode` (enum Fixed/Pool) và property tương ứng.
- `PoolCategoryId`, `PoolQuestionCount`.
- `NegativeMarkingRatio`, method `ConfigureNegativeMarking`.
- `_questionIds`/`QuestionIds`, method `AddQuestion`/`RemoveQuestion` (chọn tay kiểu Fixed).

**Thêm:**
- `Composition`: danh sách các ô `(Level Level, QuestionType QuestionType, int Count)` — value object mới, ví dụ tên `ExamCompositionCell`.
- Method `ConfigureComposition(IReadOnlyCollection<ExamCompositionCell> cells)` thay cho `ConfigureQuestionPool` — validate: mỗi `Count >= 0`, tổng `Count > 0`, chỉ gọi được khi đề còn Draft (giữ `EnsureEditable()` như các method cấu hình khác).
- `NumberOfQuestions => Composition.Sum(c => c.Count)` (thay biểu thức cũ dựa trên `QuestionSelectionMode`).

**Đổi kiểu:**
- `MinimumPassingScore`: `int` → `decimal`, validate trong khoảng `[0, 10]` (thay vì chỉ `>= 0` như hiện tại).

**Giữ nguyên không đổi:** `Duration`, `Level` (tag hiển thị tổng thể của đề thi, độc lập với ma trận), `IsTimeRestricted`, `CategoryId`/`CategoryName`, `AvailableFrom/To`+`ScheduleAvailability`, `MaxAttempts`+`ConfigureMaxAttempts`, `AssignedClassIds`+`AssignToClass`/`UnassignFromClass`, `IsPublic`, `Publish`/`Unpublish`/`Archive` (vẫn chặn `Publish()` nếu `NumberOfQuestions == 0`, chỉ nguồn dữ liệu đổi).

### 2. Rút câu hỏi (`ExamQuestionPoolService.cs`)

Mở rộng `DrawQuestionIdsAsync`: lặp qua từng ô trong `exam.Composition`, với mỗi ô rút ngẫu nhiên đúng `Count` câu hỏi thuộc đúng `exam.CategoryId` + `cell.Level` + `cell.QuestionType` (dùng lại `Random.Shared.Shuffle` như hiện tại, chỉ chạy theo từng ô thay vì 1 lần cho cả đề). Nếu ngân hàng câu hỏi không đủ số lượng cho 1 ô, ném `ExamDomainException` nêu rõ đang thiếu ô nào (mức độ + loại), theo đúng tinh thần thông báo lỗi Pool hiện tại — chỉ chi tiết hoá theo từng ô thay vì 1 thông báo chung chung.

### 3. Ngân hàng câu hỏi (`Question.cs`, `QuestionDto`, `QuestionRequest`)

Xoá hoàn toàn field `Points` khỏi: entity `Question` (constructor, `Update`, method `ChangePoints`, validate trong `EnsureValid`), `QuestionDto`/`QuestionRequest` (Exam.Contracts), UI nhập câu hỏi (`QuestionFormDialog.razor.cs:30` hiện đang bind `points = Model.Points`), và template import/export Excel (cột Points trong `ImportQuestionsDialog`/service export).

### 4. Chấm điểm (`ExamResult.cs`, `QuestionResult.cs`, `ExamResultGradingService.cs`)

- `QuestionResult`: xoá field `Points` (constructor không nhận `points` nữa).
- `ExamResult`: xoá `NegativeMarkingRatio` và constructor param tương ứng. Xoá nhánh trừ điểm trong `ScoreFor` — hàm mới: câu đúng góp `1`, câu sai/bỏ trống góp `0` (đếm số câu đúng, không tính điểm từng câu riêng lẻ nữa).
  - `TotalScore => Math.Round((decimal)CorrectQuestionCount / QuestionResults.Count * 10m, 2)` (bảo vệ chia-cho-0 nếu `QuestionResults.Count == 0` — về lý thuyết không xảy ra vì `Publish()` đã chặn đề rỗng, nhưng vẫn nên có guard rõ ràng thay vì để throw `DivideByZeroException` khó hiểu).
  - `MaxPossibleScore => 10m` (giữ lại làm property tiện lợi — hằng số, không còn tính tổng `Points` — để `ExamResultPage.razor`'s binding "@TotalScore/@MaxPossibleScore điểm" không cần sửa gì).
- `ExamResultGradingService.GradeAndFinishAsync`: đơn giản hoá — không còn cần đọc `Points` hay `NegativeMarkingRatio` từ `Exam`/`Question`, chỉ cần tạo `QuestionResult` (không `Points`) và gọi `examResult.Finish(exam.MinimumPassingScore)` như cũ (so sánh trực tiếp với `TotalScore` decimal 0–10).

### 5. Giao diện (`Exam.WebApp`)

- **`ExamFormDialog.razor`**: xoá switch "Trừ điểm khi chọn sai" và ô nhập tỉ lệ trừ điểm. Xoá switch "Chọn câu hỏi ngẫu nhiên theo pool" + ô nhập số lượng pool đơn — thay bằng 1 bảng ma trận nhỏ: 3 hàng (Dễ/Vừa/Khó) × 2 cột (Một đáp án/Nhiều đáp án), mỗi ô 1 `AppNumericField Min="0"`, hiển thị tổng số câu realtime bên dưới bảng. Ô "Điểm đạt tối thiểu" đổi `AppNumericField TValue="int"` → `TValue="decimal"`, thêm `Max="10"`, `Step="0.5"`.
- **`Exams.razor`/`Exams.razor.cs`** (trang chi tiết đề thi): xoá toàn bộ khu vực chọn câu hỏi tay kiểu Fixed (`categoryQuestions`, `ToggleQuestionAsync`, card checklist câu hỏi) và card "Trừ điểm khi chọn sai". Thay card cấu hình Pool hiện tại bằng bảng ma trận tương tự `ExamFormDialog` (sửa được khi đề còn Draft, giữ đúng logic khoá field theo `CanManageSelected`/trạng thái đề đã có).
- **`QuestionFormDialog.razor`**: xoá ô nhập Points.
- **`ImportQuestionsDialog.razor`/Excel template**: xoá cột Points khỏi cả preview lẫn export mẫu.

### 6. API/Command/Permission

- **Xoá command + endpoint:** thêm-câu-hỏi-vào-đề và bỏ-câu-hỏi-khỏi-đề (Fixed mode — 2 endpoint tại `ExamsController.cs:92,100` hiện gate bởi `Permissions.Exam.ManageQuestions`); cấu-hình-trừ-điểm (`ExamsController.cs:126`, gate bởi `Permissions.Exam.ManageNegativeMarking`).
- **Đổi tên:** command `ConfigureQuestionPool` → `ConfigureExamComposition`, nhận vào danh sách ô thay vì `(poolCategoryId, questionCount)`. Endpoint tại `ExamsController.cs:108` vẫn gate bởi `Permissions.Exam.ManagePool` (giữ nguyên tên hằng số permission để không phải cấp lại quyền cho các role đã gán, dù ý nghĩa chuyển thành "quản lý ma trận cấu trúc").
- **`Permissions.cs`**: xoá `Exam.ManageQuestions`, `Exam.ManageNegativeMarking` khỏi class `Exam` và mảng `All`. `RolePermissionSeeder` tự động không seed 2 quyền này nữa ở lần khởi động tiếp theo (theo cơ chế diff đã có sẵn — không cần code thêm), nhưng **quyền đã gán sẵn cho role trong DB không tự mất** — cần 1 bước dọn dữ liệu `rolePermissions` (xem mục Dữ liệu cũ).

### 7. Dữ liệu cũ

Vì xác nhận chưa có đề thi thật cần giữ: kế hoạch triển khai sẽ có bước dọn dữ liệu MongoDB thủ công **trước khi** bật code mới:
- Xoá sạch collection `exams` (cấu trúc Fixed/Pool cũ không tương thích với `Composition` mới).
- Xoá sạch collection `examResults` (tham chiếu tới các đề thi cũ sắp bị xoá).
- Collection `questions` **giữ nguyên** — field `points` cũ trong các document hiện có sẽ đơn giản không còn được code mới đọc/ghi tới (Mongo là schemaless, không cần script dọn field thừa).
- Collection `rolePermissions`: loại bỏ giá trị `"Exam.ManageQuestions"`/`"Exam.ManageNegativeMarking"` khỏi mảng quyền của mọi role đã gán (nếu có) — tránh rác quyền trỏ tới permission không còn tồn tại trong `Permissions.All`.

## Ngoài phạm vi (deferred)

- **Học viên xem danh sách bạn cùng lớp** — ý tưởng nêu ra trong cùng buổi trao đổi nhưng là tính năng độc lập, chưa brainstorm chi tiết (còn treo: có hiển thị email bạn cùng lớp hay chỉ tên). Sẽ brainstorm riêng sau khi spec này được duyệt.
- Các gap nghiệp vụ khác đã nêu trong đợt rà soát trước đó (không có đăng ký/tạo user, không có email/thông báo, không xuất được bảng điểm, không có chống gian lận lúc thi, chỉ 2 dạng câu hỏi, không nhập roster hàng loạt, không có quy trình phúc khảo) — không nằm trong phạm vi đợt này.
- Trọng số điểm theo độ khó (khó > vừa > dễ) — người dùng đã chọn phương án chia đều, phương án trọng số bị loại ở bước brainstorm, ghi lại ở đây để không bị đề xuất lại nhầm là "chưa cân nhắc".

## Rủi ro & Kế hoạch xác minh

- Không viết unit test (quy ước dự án). Xác minh bằng `dotnet build` cho cả 2 phía (Exam.API + Exam.WebApp) và thao tác tay: tạo đề mới qua ma trận, publish, làm bài với tài khoản Student, xác nhận điểm hiển thị đúng thang 0–10 và đúng công thức chia đều.
- Rủi ro chính: sai lệch giữa số lượng ô ma trận yêu cầu và số câu hỏi thực có trong ngân hàng theo từng (Level, QuestionType) — cần test rõ thông báo lỗi khi thiếu câu hỏi ở 1 ô cụ thể, đặc biệt với môn học ít câu hỏi trong seed/test data hiện tại.
- Thay đổi này **xoá dữ liệu đề thi hiện có** — cần xác nhận lại với người dùng ngay trước khi chạy bước dọn dữ liệu trong kế hoạch triển khai (không tự động xoá khi chưa được duyệt tường minh ở bước thực thi).
