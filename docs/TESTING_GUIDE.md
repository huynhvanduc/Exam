# Tài liệu Test — Exam Platform API

Tài liệu này dành cho tester, hướng dẫn cách chuẩn bị môi trường, lấy token xác thực, và test các API đã triển khai (Category, Question, Exam, ExamAttempts).

> **Postman:** Có sẵn bộ collection + environment import thẳng được tại `docs/postman/Exam-API.postman_collection.json` và `docs/postman/Exam-API.postman_environment.json` — đầy đủ happy-path, negative case, và test script tự động (`pm.test`), dùng chung data thật đã seed liệt kê ở mục 5. Import cả 2 file vào Postman, chọn environment "Exam API - Local", chạy folder "00 - Auth" trước để lấy token.
>
> ⚠️ File Postman hiện đang trỏ **id cũ** (trước khi hệ thống chuyển sang chạy Docker) — id trong tài liệu này (mục 5) là bản mới nhất. Nếu chạy Postman thấy 404, dùng id trong tài liệu này thay thế.

---

## 1. Tổng quan hệ thống

Toàn bộ hệ thống giờ chạy bằng **Docker Compose** — không cần `dotnet run` thủ công nữa.

| Container | Vai trò | Port (host) |
|---|---|---|
| `identity.server` | Identity Server (IdentityServer4) + **UI đăng nhập bằng Blazor** | `http://localhost:5001` |
| `exam.api` | API nghiệp vụ chính (Category/Question/Exam/ExamAttempts) + Health Check UI | `http://localhost:5000` |
| `mongo.db` | Lưu dữ liệu nghiệp vụ (Category/Question/Exam/ExamResult) | `localhost:27017` |
| `sqlserver.db` | Lưu dữ liệu Identity Server (user, client, scope) | `localhost:1433` |

Kiến trúc `Exam.API`: Domain → Application (CQRS/MediatR) → API (REST Controllers), theo Category → Question → Exam → ExamResult (4 aggregate đã triển khai đầy đủ).

⚠️ **Lưu ý môi trường máy dev hiện tại:** máy đang có sẵn 1 **Windows Service "MongoDB" chạy native**, tách biệt với container `mongo.db`. Nếu bạn tự chạy `dotnet run` (không qua Docker), rất dễ vô tình nối nhầm vào service native đó (không cần auth, data khác hoàn toàn với data trong tài liệu này). Khuyến nghị **luôn dùng Docker** (mục 2) để chắc chắn đang test đúng data.

---

## 2. Chuẩn bị môi trường (Docker)

```bash
docker compose up -d
```

Lệnh này khởi động cả 4 container. Kiểm tra:
```bash
docker compose ps
```

Xác nhận: `GET http://localhost:5000/health` phải trả `200` (endpoint duy nhất **không** cần token).

Xem log nếu có lỗi: `docker logs exam.api` / `docker logs identity.server`.

---

## 3. Xác thực (Authentication)

**Toàn bộ API của `Exam.API` (trừ `/health`, `/healthchecks-ui*`) đều yêu cầu Bearer token hợp lệ** phát hành bởi Identity Server (audience = `exam_api`).

### ✅ Login UI đã có (Blazor) — cập nhật so với trước

`Identity.Server` giờ đã có trang đăng nhập thật tại `http://localhost:5001/Account/Login`, xây bằng Blazor. Có thể đăng nhập trực tiếp qua trình duyệt với tài khoản admin đã seed sẵn:
- **Username:** `admin`
- **Password:** `Admin@123$`

Không còn màn hình Consent (đã tắt cho 3 client nội bộ) — đăng nhập xong là quay thẳng về `redirect_uri` kèm `code`.

### Cách lấy token để test

**Cách 1 — nhanh, dùng cho test API thuần (Category/Question/Exam):** client `exam.tester` (`client_credentials`, không gắn user thật):

```bash
curl -X POST http://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=exam.tester&client_secret=TesterSecret123$&scope=exam_api.read%20exam_api.write"
```

⚠️ Token này **không có claim `sub`** (không đại diện user thật) → gọi vào nhóm `ExamAttempts` sẽ nhận `400 Bad Request: 'User Id' must not be empty.` — đây là hành vi đúng (chặn giả danh user), không phải bug.

**Cách 2 — đầy đủ, dùng cho test luồng `ExamAttempts`:** đăng nhập qua trình duyệt tại client Swagger UI:
```
http://localhost:5001/connect/authorize?client_id=exam_api_swaggerui&response_type=code&scope=openid%20profile%20exam_api.read%20exam_api.write&redirect_uri=http://localhost:5002/swagger/oauth2-redirect.html&code_challenge=...&code_challenge_method=S256&state=xyz
```
(cần PKCE — thuận tiện nhất là dùng Postman's OAuth 2.0 helper trong tab Authorization, hoặc trình duyệt + DevTools để lấy `code` rồi đổi qua `/connect/token`). Token trả về lúc này có `sub` thật → gọi được toàn bộ `ExamAttempts`.

Response trả về `access_token` — dùng làm header cho mọi request:
```
Authorization: Bearer <access_token>
```

Token hết hạn sau **3600 giây (1 giờ)**.

> Client chỉ-đọc (nếu cần test riêng quyền read-only): `client_id=exam.warehouse.worker`, `client_secret=SuperSecretPassword`, `scope=exam_api.read` — API hiện **chưa enforce theo scope** (xem mục 7), nên token này vẫn gọi được POST/PUT/DELETE.

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

## 5. Dữ liệu mẫu có sẵn (đã seed trong container `mongo.db`)

### Categories
| Id | Tên | urlPath |
|---|---|---|
| `6a53470e293ee3333adb945f` | Lập trình C# | csharp |
| `6a53470f293ee3333adb9460` | Cơ sở dữ liệu | database |
| `6a53470f293ee3333adb9461` | Mạng máy tính | networking |

### Questions — Category "Lập trình C#" (5 câu)
| Id câu hỏi | Nội dung | Loại | Đáp án đúng (id) |
|---|---|---|---|
| `6a53470f293ee3333adb9466` | C# là ngôn ngữ lập trình theo hướng nào? | SingleSelection, Easy, 1đ | `6a53470f293ee3333adb9462` (Hướng đối tượng) |
| `6a53470f293ee3333adb946b` | Từ khóa nào dùng để kế thừa một lớp trong C#? | SingleSelection, Easy, 1đ | `6a53470f293ee3333adb9469` (Dấu hai chấm) |
| `6a53470f293ee3333adb9470` | LINQ là viết tắt của gì? | SingleSelection, Medium, 2đ | `6a53470f293ee3333adb946c` (Language Integrated Query) |
| `6a53470f293ee3333adb9475` | async/await trong C# dùng để làm gì? | SingleSelection, Medium, 2đ | `6a53470f293ee3333adb9471` (Lập trình bất đồng bộ) |
| `6a53470f293ee3333adb947a` | Kiểu dữ liệu nào là reference type? | MultipleSelection, Difficult, 3đ | `6a53470f293ee3333adb9476` + `6a53470f293ee3333adb9477` (class, string) |

### Questions — Category "Cơ sở dữ liệu" (5 câu)
| Id câu hỏi | Nội dung | Đáp án đúng |
|---|---|---|
| `6a53470f293ee3333adb947f` | SQL là viết tắt của gì? | `6a53470f293ee3333adb947b` |
| `6a53470f293ee3333adb9484` | Khóa chính (Primary Key) có đặc điểm gì? | `6a53470f293ee3333adb9480` |
| `6a53470f293ee3333adb9489` | Chuẩn hóa dữ liệu (Normalization) nhằm mục đích gì? | `6a53470f293ee3333adb9485` |
| `6a53470f293ee3333adb948e` | Lệnh nào xóa dữ liệu nhưng giữ cấu trúc bảng? | `6a53470f293ee3333adb948a` (TRUNCATE TABLE) |
| `6a53470f293ee3333adb9493` | Index trong CSDL giúp gì? | `6a53470f293ee3333adb948f` |

### Questions — Category "Mạng máy tính" (4 câu)
| Id câu hỏi | Nội dung | Đáp án đúng |
|---|---|---|
| `6a53470f293ee3333adb9498` | Mô hình OSI có bao nhiêu tầng? | `6a53470f293ee3333adb9494` (7) |
| `6a534710293ee3333adb949d` | Giao thức nào phân giải tên miền → IP? | `6a534710293ee3333adb9499` (DNS) |
| `6a534710293ee3333adb94a2` | TCP khác UDP ở điểm nào? | `6a534710293ee3333adb949e` |
| `6a534710293ee3333adb94a7` | Cổng mặc định của HTTPS là gì? | `6a534710293ee3333adb94a3` (443) |

*(Gọi `GET /api/questions/by-category/{categoryId}` để lấy JSON đầy đủ, có tất cả 4 đáp án của từng câu — bảng trên chỉ liệt kê đáp án đúng để tiện test.)*

### Exams
| Id | Tên | Mode | Trạng thái | Số câu |
|---|---|---|---|---|
| `6a534710293ee3333adb94a8` | Kiểm tra C# cơ bản | Fixed | **Published** | 4 |
| `6a534710293ee3333adb94a9` | Đề thi tổng hợp CSDL | Pool (rút 3/5 câu ngẫu nhiên) | **Published** | 3 |
| `6a534710293ee3333adb94aa` | Kiểm tra mạng máy tính | Fixed | **Draft** (chưa publish — dùng để test case "không cho làm bài đề chưa publish") | 4 |

### ExamResult mẫu (sinh qua đăng nhập thật, không phải mô phỏng)
- Id: `6a5347e1293ee3333adb94ad`, user `sub=528ac9b1-20ff-4d42-bd6d-e85300acde89` (tài khoản `admin`), làm đề "Kiểm tra C# cơ bản"
- Kết quả: `totalScore=3/6`, `passed=true`, đúng 2/4 câu (2 câu cố tình trả lời sai để minh hoạ cách chấm điểm)

---

## 6. Tham chiếu API

Tất cả request dưới đây cần header `Authorization: Bearer <token>` (trừ `/health`, `/healthchecks-ui*`).

### 6.0 Vận hành / Giám sát (public, không cần token)
| Method | Path | Ghi chú |
|---|---|---|
| GET | `/health` | JSON chi tiết trạng thái từng health check |
| GET | `/healthchecks-ui` | Dashboard trực quan (mở bằng trình duyệt) |

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

### 6.4 ExamAttempts (`/api/exam-attempts`) — cần token có `sub` (mục 3, cách 2)
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
| 1 | Enum trả về số, không phải chuỗi | Phải tra bảng mục 4 khi đọc response |
| 2 | Chưa enforce theo `scope` (`exam_api.read` vs `.write`) | Token chỉ-đọc vẫn gọi được POST/PUT/DELETE — nếu cần test phân quyền theo scope, đây là gap cần báo dev |
| 3 | Chưa có API quản lý `Role` user (promote Student→Instructor/Admin) | `UserAggregate` mới chỉ có auto-provision, chưa có CRUD |
| 4 | Không có job tự nộp bài đúng giờ (dùng "lazy expiry" — chỉ auto-finish khi có request tiếp theo chạm tới) | Nếu test case "hết giờ tự nộp", cần gọi lại `GET /api/exam-attempts/{id}` hoặc `POST .../answers` sau khi hết `deadline` để trigger, không tự động ngay lúc hết giờ |
| 5 | `FirstName/LastName` chưa vào JWT (ASP.NET Identity default không map custom claim vào token) | User tự động provision lúc login lần đầu sẽ có tên hiển thị "Unknown User" trong `ExamResult.fullName` — không phải bug, chỉ là chưa hoàn thiện profile sync |
| 6 | 2 MongoDB song song trên máy dev (xem mục 1) | Nếu không chạy qua Docker, rất dễ test nhầm data |

---

## 8. Checklist kịch bản test đề xuất

- [ ] CRUD Category — tạo/sửa/xoá, xoá category đang có Question/Exam phải bị chặn (400)
- [ ] CRUD Question — tạo với `SingleSelection` có 2 đáp án đúng phải bị từ chối (400); tạo với `categoryId` không tồn tại phải 404
- [ ] CRUD Exam — publish đề chưa có câu hỏi phải bị chặn; sửa đề đã publish phải bị chặn
- [ ] `ConfigureQuestionPool` với `poolQuestionCount` lớn hơn số câu hỏi thực tế trong category → phải báo lỗi rõ ràng
- [ ] Gọi bất kỳ API nào **không kèm token** → phải nhận `401`
- [ ] Gọi API với token hết hạn → phải nhận `401`
- [ ] Đăng nhập qua `http://localhost:5001/Account/Login` (admin/Admin@123$), lấy token thật → luồng làm bài đầy đủ: Start → RecordAnswer từng câu → Finish → so khớp điểm với `points` + `minimumPassingScore` của đề
- [ ] `GET /healthchecks-ui` hiển thị đúng trạng thái Healthy khi mọi container đang chạy
