# Tài liệu Test — Exam Platform API

Tài liệu này dành cho tester, hướng dẫn cách chuẩn bị môi trường, lấy token xác thực, và test các API đã triển khai (Category, Question, Exam, ExamAttempts).

> **Postman:** Có sẵn bộ collection + environment import thẳng được tại `docs/postman/Exam-API.postman_collection.json` và `docs/postman/Exam-API.postman_environment.json` — đầy đủ happy-path, negative case, và test script tự động (`pm.test`), dùng chung data thật đã seed liệt kê ở mục 5. Import cả 2 file vào Postman, chọn environment "Exam API - Local", chạy folder "00 - Auth" trước để lấy token.

---

## 1. Tổng quan hệ thống

| Thành phần | Vai trò | Port (dev) |
|---|---|---|
| `Identity.API` | Identity Server (IdentityServer4), phát hành JWT token | `http://localhost:5001` |
| `Exam.API` | API nghiệp vụ chính (Category/Question/Exam/ExamAttempts) | `http://localhost:5000` |
| MongoDB | Lưu dữ liệu nghiệp vụ (Category/Question/Exam/ExamResult) | `localhost:27017` |
| SQL Server | Lưu dữ liệu Identity Server (user, client, scope) | `localhost:1433` |

Kiến trúc `Exam.API`: Domain → Application (CQRS/MediatR) → API (REST Controllers), theo Category → Question → Exam → ExamResult (4 aggregate đã triển khai đầy đủ).

---

## 2. Chuẩn bị môi trường

1. Đảm bảo MongoDB và SQL Server đang chạy (xem `docker-compose.yml` ở root repo, hoặc dùng instance local sẵn có).
2. Chạy Identity Server:
   ```
   dotnet run --project src/Identity/Identity.API --urls http://localhost:5001
   ```
3. Chạy Exam API:
   ```
   dotnet run --project src/Exam/Exam.API --urls http://localhost:5000
   ```
4. Kiểm tra: `GET http://localhost:5000/health` phải trả `200` (endpoint duy nhất **không** cần token).

---

## 3. Xác thực (Authentication)

**Toàn bộ API của `Exam.API` (trừ `/health`) đều yêu cầu Bearer token hợp lệ** phát hành bởi Identity Server (audience = `exam_api`).

### ⚠️ Giới hạn quan trọng cần biết trước khi test

`Identity.API` **chưa có trang đăng nhập (login UI)** — `AddRazorPages()` đang bị comment trong `Program.cs`, không có `AccountController`. Vì vậy:

- **Không thể** test qua luồng đăng nhập tương tác (`exam.webapp`, `exam.webadmin`, `exam_api_swaggerui` — cả 3 client này dùng `authorization_code` + PKCE, cần trang login mà hiện chưa tồn tại).
- **Chỉ dùng được** `client_credentials` (machine-to-machine, không gắn với user cụ thể) để lấy token test.
- Hệ quả: **các API quản lý nội dung (Category/Question/Exam) test được đầy đủ**. Riêng nhóm **`ExamAttempts` (luồng làm bài thi) hiện KHÔNG test được qua Postman/token thường**, vì cần claim `sub` (định danh user thật) mà `client_credentials` token không có — gọi vào sẽ nhận lỗi `400 Bad Request: 'User Id' must not be empty.` (đã kiểm chứng, không phải bug, là hành vi đúng do thiếu user thật). Xem mục 7 (Known Issues).

### Cách lấy token để test

Dùng client `exam.tester` (được tạo riêng cho mục đích test, có đủ 2 scope `exam_api.read` + `exam_api.write`):

```bash
curl -X POST http://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=exam.tester&client_secret=TesterSecret123$&scope=exam_api.read%20exam_api.write"
```

Response trả về `access_token` — dùng làm header cho mọi request:
```
Authorization: Bearer <access_token>
```

Token hết hạn sau **3600 giây (1 giờ)** — hết hạn thì lấy lại bằng lệnh trên.

> Có sẵn 1 client chỉ đọc nếu cần test riêng quyền read-only: `client_id=exam.warehouse.worker`, `client_secret=SuperSecretPassword`, `scope=exam_api.read` (không có quyền ghi, dùng để test case "ghi bị từ chối" nếu cần — hiện tại API chưa phân biệt scope trong `[Authorize]`, chỉ yêu cầu authenticated, nên token này vẫn gọi POST/PUT/DELETE được, chưa enforce theo scope).

---

## 4. Bảng mapping Enum

API trả enum dưới dạng **số nguyên** (chưa cấu hình `JsonStringEnumConverter` — xem mục 7). Dùng bảng dưới để đọc/tạo dữ liệu:

| Enum | 0 | 1 | 2 |
|---|---|---|---|
| `Level` | Easy | Medium | Difficult | |
| `QuestionType` | SingleSelection | MultipleSelection | |
| `ExamStatus` | Draft | Published | Archived |
| `QuestionSelectionMode` | Fixed | Pool | |
| `UserRole` | Student | Instructor | Admin |

---

## 5. Dữ liệu mẫu có sẵn (đã seed sẵn trong MongoDB)

### Categories
| Id | Tên | urlPath |
|---|---|---|
| `6a53151dd16b30fc16694d9d` | Lập trình C# | csharp |
| `6a53151dd16b30fc16694d9e` | Cơ sở dữ liệu | database |
| `6a53151dd16b30fc16694d9f` | Mạng máy tính | networking |

*(Ngoài ra còn 3 category cũ không do mình tạo: "Toan Hoc", "Hoa Hoc", "string" — dữ liệu thử nghiệm trước đó, có thể bỏ qua.)*

### Questions — Category "Lập trình C#" (5 câu)
| Id câu hỏi | Nội dung | Loại | Đáp án đúng (id) |
|---|---|---|---|
| `...694da4` | C# là ngôn ngữ lập trình theo hướng nào? | SingleSelection, Easy, 1đ | `...694da0` (Hướng đối tượng) |
| `...694da9` | Từ khóa nào dùng để kế thừa một lớp trong C#? | SingleSelection, Easy, 1đ | `...694da7` (Dấu hai chấm) |
| `...694dae` | LINQ là viết tắt của gì? | SingleSelection, Medium, 2đ | `...694daa` (Language Integrated Query) |
| `...694db3` | async/await trong C# dùng để làm gì? | SingleSelection, Medium, 2đ | `...694daf` (Lập trình bất đồng bộ) |
| `...694db8` | Kiểu dữ liệu nào là reference type? | MultipleSelection, Difficult, 3đ | `...694db4` + `...694db5` (class, string) |

*(prefix đầy đủ của mọi id câu hỏi/đáp án bên trên: `6a53151dd16b30fc16` — ví dụ `...694da4` = `6a53151dd16b30fc16694da4`)*

### Questions — Category "Cơ sở dữ liệu" (5 câu)
| Id câu hỏi | Nội dung | Đáp án đúng |
|---|---|---|
| `...694dbd` | SQL là viết tắt của gì? | `...694db9` |
| `...694dc2` | Khóa chính (Primary Key) có đặc điểm gì? | `...694dbe` |
| `...694dc7` | Chuẩn hóa dữ liệu (Normalization) nhằm mục đích gì? | `...694dc3` |
| `...694dcc` | Lệnh nào xóa dữ liệu nhưng giữ cấu trúc bảng? | `...694dc8` (TRUNCATE TABLE) |
| `...694dd1` | Index trong CSDL giúp gì? | `...694dcd` |

### Questions — Category "Mạng máy tính" (4 câu)
| Id câu hỏi | Nội dung | Đáp án đúng |
|---|---|---|
| `...694dd6` | Mô hình OSI có bao nhiêu tầng? | `...694dd2` (7) |
| `...694ddb` | Giao thức nào phân giải tên miền → IP? | `...694dd7` (DNS) |
| `...694de0` | TCP khác UDP ở điểm nào? | `...694ddc` |
| `...694de5` | Cổng mặc định của HTTPS là gì? | `...694de1` (443) |

*(Gọi `GET /api/questions/by-category/{categoryId}` để lấy JSON đầy đủ, có tất cả 4 đáp án của từng câu — bảng trên chỉ liệt kê đáp án đúng để tiện test.)*

### Exams
| Id | Tên | Mode | Trạng thái | Số câu |
|---|---|---|---|---|
| `6a53151dd16b30fc16694de6` | Kiểm tra C# cơ bản | Fixed | **Published** | 4 |
| `6a53151dd16b30fc16694de7` | Đề thi tổng hợp CSDL | Pool (rút 3/5 câu ngẫu nhiên) | **Published** | 3 |
| `6a53157dd16b30fc16694de8` | Kiểm tra mạng máy tính | Fixed | **Draft** (chưa publish — dùng để test case "không cho làm bài đề chưa publish") | 4 |

### ExamResult mẫu (sinh trước khi bật auth, tham khảo shape dữ liệu)
- Id: `6a53157dd16b30fc16694de9`, làm đề "Kiểm tra C# cơ bản", user "Nguyễn Văn A"
- Kết quả: `totalScore=3/6`, `passed=true`, đúng 2/4 câu (2 câu cố tình trả lời sai để test negative case)

---

## 6. Tham chiếu API

Tất cả request dưới đây cần header `Authorization: Bearer <token>` (trừ `/health`).

### 6.1 Categories (`/api/categories`)
| Method | Path | Body mẫu |
|---|---|---|
| POST | `/api/categories` | `{"name":"Vật lý","urlPath":"vat-ly"}` |
| GET | `/api/categories` | — |
| GET | `/api/categories/{id}` | — |
| GET | `/api/categories/by-url-path/{urlPath}` | — |
| PUT | `/api/categories/{id}` | `{"name":"...","urlPath":"..."}` |
| DELETE | `/api/categories/{id}` | — (400 nếu còn Question/Exam tham chiếu) |

### 6.2 Questions (`/api/questions`)
| Method | Path | Body mẫu |
|---|---|---|
| POST | `/api/questions` | `{"content":"...","questionType":0,"level":0,"categoryId":"...","answers":[{"content":"A","isCorrect":true},{"content":"B","isCorrect":false}],"explain":"...","points":1,"ownerUserId":"teacher-1"}` |
| GET | `/api/questions/{id}` | — |
| GET | `/api/questions/by-category/{categoryId}` | — |
| PUT | `/api/questions/{id}` | giống body Create, không có `ownerUserId` |
| DELETE | `/api/questions/{id}` | — |

Validate cần nhớ: `SingleSelection` chỉ được đúng 1 đáp án `isCorrect=true`; phải có ít nhất 1 đáp án đúng; `categoryId` phải tồn tại (404 nếu không).

### 6.3 Exams (`/api/exams`)
| Method | Path | Body mẫu |
|---|---|---|
| POST | `/api/exams` | `{"name":"...","shortDesc":"...","content":"...","duration":"00:30:00","level":0,"categoryId":"...","isTimeRestricted":true,"minimumPassingScore":2,"ownerUserId":"teacher-1"}` |
| GET | `/api/exams/{id}` | — |
| GET | `/api/exams/by-category/{categoryId}` | — |
| PUT | `/api/exams/{id}` | giống body Create, không có `ownerUserId` |
| DELETE | `/api/exams/{id}` | — |
| POST | `/api/exams/{id}/questions/{questionId}` | — (thêm câu hỏi, chỉ khi `Draft`) |
| DELETE | `/api/exams/{id}/questions/{questionId}` | — |
| PUT | `/api/exams/{id}/question-pool` | `{"poolCategoryId":"...","poolQuestionCount":3}` |
| PUT | `/api/exams/{id}/availability` | `{"availableFrom":null,"availableTo":null}` |
| PUT | `/api/exams/{id}/negative-marking` | `{"ratio":0.25}` |
| POST | `/api/exams/{id}/publish` | — |
| POST | `/api/exams/{id}/unpublish` | — |
| POST | `/api/exams/{id}/archive` | — |

Business rule cần test: không publish được đề chưa có câu hỏi; không sửa được đề đã `Published` (phải `Unpublish` trước); `ConfigureQuestionPool` báo lỗi nếu category không đủ câu hỏi.

### 6.4 ExamAttempts (`/api/exam-attempts`) — **⚠️ hiện không test được qua token thường, xem mục 3**
| Method | Path | Body mẫu |
|---|---|---|
| POST | `/api/exam-attempts/start` | `{"examId":"..."}` |
| GET | `/api/exam-attempts/{id}` | — |
| POST | `/api/exam-attempts/{id}/answers` | `{"questionId":"...","selectedAnswerIds":["..."]}` |
| POST | `/api/exam-attempts/{id}/finish` | — |
| GET | `/api/exam-attempts/{id}/result` | — |
| GET | `/api/exam-attempts/history` | — |

---

## 7. Known Issues / Ngoài phạm vi lần này

| # | Vấn đề | Ảnh hưởng tới test |
|---|---|---|
| 1 | `Identity.API` chưa có login UI | Không test được `ExamAttempts` qua token thường; chỉ test được qua code review/kiểm thử nội bộ đã verify trước đó |
| 2 | Enum trả về số, không phải chuỗi | Phải tra bảng mục 4 khi đọc response |
| 3 | Chưa enforce theo `scope` (`exam_api.read` vs `.write`) | Token chỉ-đọc vẫn gọi được POST/PUT/DELETE — nếu cần test phân quyền theo scope, đây là gap cần báo dev |
| 4 | Chưa có API quản lý `Role` user (promote Student→Instructor/Admin) | `UserAggregate` mới chỉ có auto-provision, chưa có CRUD |
| 5 | Không có job tự nộp bài đúng giờ (dùng "lazy expiry" — chỉ auto-finish khi có request tiếp theo chạm tới) | Nếu test case "hết giờ tự nộp", cần gọi lại `GET /api/exam-attempts/{id}` hoặc `POST .../answers` sau khi hết `deadline` để trigger, không tự động ngay lúc hết giờ |

---

## 8. Checklist kịch bản test đề xuất

- [ ] CRUD Category — tạo/sửa/xoá, xoá category đang có Question/Exam phải bị chặn (400)
- [ ] CRUD Question — tạo với `SingleSelection` có 2 đáp án đúng phải bị từ chối (400); tạo với `categoryId` không tồn tại phải 404
- [ ] CRUD Exam — publish đề chưa có câu hỏi phải bị chặn; sửa đề đã publish phải bị chặn
- [ ] `ConfigureQuestionPool` với `poolQuestionCount` lớn hơn số câu hỏi thực tế trong category → phải báo lỗi rõ ràng
- [ ] Gọi bất kỳ API nào **không kèm token** → phải nhận `401`
- [ ] Gọi API với token hết hạn → phải nhận `401`
- [ ] (Khi có login UI) Luồng làm bài đầy đủ: Start → RecordAnswer từng câu → Finish → so khớp điểm với `points` + `minimumPassingScore` của đề
