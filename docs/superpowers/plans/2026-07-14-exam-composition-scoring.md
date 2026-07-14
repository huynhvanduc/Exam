# Ma trận cấu trúc đề thi & thang điểm 10 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay hai chế độ chọn câu hỏi Fixed/Pool bằng một ma trận cấu trúc đề thi (mức độ × loại câu hỏi), chấm điểm luôn ra đúng thang 0–10 bằng công thức chia đều, và bỏ hẳn trừ điểm.

**Architecture:** Mở rộng trực tiếp aggregate `Exam` (thêm value object `ExamCompositionCell` + `Composition`) thay vì tách aggregate riêng; `ExamQuestionPoolService` rút ngẫu nhiên theo từng ô; `ExamResult.TotalScore` tính `(số câu đúng / tổng số câu) × 10`. Backend là CQRS/MediatR trên MongoDB; frontend là Blazor Server (Admin). Đây là thay đổi phá vỡ tương thích ngược (không giữ dữ liệu đề thi cũ) nên mỗi task là một lát cắt dọc tự biên dịch xanh (`dotnet build`) khi hoàn tất, xếp theo mức rủi ro tăng dần.

**Tech Stack:** .NET 8, MediatR/CQRS, FluentValidation, MongoDB (driver C#, ClosedXML cho Excel), Blazor Server (InteractiveServer), docker-compose để chạy và kiểm tra thủ công.

## Global Constraints

- **Không viết unit test** (quy ước dự án). Xác minh bằng `dotnet build` cho toàn solution (Exam.API + Exam.WebApp) và thao tác tay trên stack docker-compose. Mọi bước "how to verify" là lệnh build và/hoặc thao tác tay cụ thể, tuyệt đối không gọi test framework.
- **Điểm luôn ra thang 0–10**: `TotalScore = (Số câu đúng / Tổng số câu) × 10`, làm tròn 2 chữ số thập phân, chia đều toàn bộ (không phân biệt trọng số độ khó). Trả lời sai hoặc bỏ trống đều = 0 điểm cho câu đó, không âm.
- **Ma trận áp dụng cho MỌI đề thi, thay thế hoàn toàn Fixed lẫn Pool** — không giữ tương thích ngược. Hệ thống tự rút ngẫu nhiên 100% theo từng ô, giảng viên không tick tay câu hỏi nữa.
- **Field `Points` trên câu hỏi bị xoá hoàn toàn**; **bỏ hẳn negative marking**.
- **Giữ nguyên tên hằng số `Permissions.Exam.ManagePool`** (ý nghĩa chuyển thành "quản lý ma trận cấu trúc") để không phải cấp lại quyền cho các role đã gán.
- **Xoá dữ liệu đề thi hiện có**: bước dọn dữ liệu MongoDB (drop `exams`/`examResults`, dọn 2 permission khỏi `rolePermissions`) chỉ chạy ở task cuối và **phải xác nhận lại tường minh với người dùng ngay trước khi chạy** (không tự động xoá khi chưa được duyệt). Collection `questions` giữ nguyên (field `points` cũ chỉ đơn giản không còn được đọc — Mongo schemaless).
- Spec gốc: `docs/superpowers/specs/2026-07-14-exam-composition-scoring-design.md`.

---

## Task 1: Chuyển `MinimumPassingScore` sang thang thập phân 0–10

**Files:**
- Modify: `src/Services/Exam/Exam.Domain/AggregateModels/ExamAggregate/Exam.cs` (property `MinimumPassingScore` `int`→`decimal`, validate `[0,10]` ở ctor dòng 83-84 và `UpdateDetails` dòng 111-112, đổi kiểu tham số 2 nơi)
- Modify: `src/Services/Exam/Exam.Domain/AggregateModels/ExamResultAggregate/ExamResult.cs` (`Finish(int)`→`Finish(decimal)`, dòng 141)
- Modify: `src/Services/Exam/Exam.Domain/Services/ExamResultGradingService.cs` (dòng 45 `?? 0`→`?? 0m`)
- Modify: `src/Services/Exam/Exam.Contracts/ExamDto.cs` (dòng 12 `int MinimumPassingScore`→`decimal`)
- Modify: `src/Services/Exam/Exam.Contracts/ExamRequest.cs` (dòng 11 `int MinimumPassingScore`→`decimal`)
- Modify: `src/Services/Exam/Exam.Application/ExamAggregate/Commands/CreateExam/CreateExamCommand.cs` + `CreateExamCommandValidator.cs`
- Modify: `src/Services/Exam/Exam.Application/ExamAggregate/Commands/UpdateExam/UpdateExamCommand.cs` + `UpdateExamCommandValidator.cs`
- Modify: `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/MongoClassMaps.cs` (thêm DecimalSerializer cho `Exam.MinimumPassingScore`)
- Modify: `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/DataSeeder.cs` (2 chỗ `minimumPassingScore:` sang decimal)
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/ExamFormDialog.razor` + `.razor.cs` (ô nhập đổi `TValue="decimal"`, field `decimal`)

**Interfaces:**
- Produces: `Exam.MinimumPassingScore : decimal`, `ExamDto.MinimumPassingScore : decimal`, `ExamRequest.MinimumPassingScore : decimal`, `ExamResult.Finish(decimal)`. Task 3 (Exam.cs full rewrite) và Task 2 (ExamResult scoring) dựa trên các kiểu này.

- [ ] **Step 1: Đổi kiểu + validate trong `Exam.cs`**

Trong `src/Services/Exam/Exam.Domain/AggregateModels/ExamAggregate/Exam.cs`, dòng 26:
```csharp
    public int MinimumPassingScore { get; private set; }
```
Thay bằng:
```csharp
    public decimal MinimumPassingScore { get; private set; }
```

Trong ctor, thay khối dòng 74-84 (chữ ký + validate):
```csharp
    public Exam(string name, string shortDesc, string content, TimeSpan duration, Level level, string ownerUserId,
        string categoryId, string categoryName, bool isTimeRestricted, int minimumPassingScore)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ExamDomainException("Exam name is required.");

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Exam category is required.");

        if (minimumPassingScore < 0)
            throw new ExamDomainException("Minimum passing score must not be negative.");
```
Thành:
```csharp
    public Exam(string name, string shortDesc, string content, TimeSpan duration, Level level, string ownerUserId,
        string categoryId, string categoryName, bool isTimeRestricted, decimal minimumPassingScore)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ExamDomainException("Exam name is required.");

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Exam category is required.");

        if (minimumPassingScore < 0 || minimumPassingScore > 10)
            throw new ExamDomainException("Minimum passing score must be between 0 and 10.");
```

Trong `UpdateDetails`, thay khối dòng 100-112 (chữ ký + validate):
```csharp
    public void UpdateDetails(string name, string shortDesc, string content, TimeSpan duration, Level level,
        string categoryId, string categoryName, bool isTimeRestricted, int minimumPassingScore)
    {
        EnsureEditable();

        if (string.IsNullOrWhiteSpace(name))
            throw new ExamDomainException("Exam name is required.");

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Exam category is required.");

        if (minimumPassingScore < 0)
            throw new ExamDomainException("Minimum passing score must not be negative.");
```
Thành:
```csharp
    public void UpdateDetails(string name, string shortDesc, string content, TimeSpan duration, Level level,
        string categoryId, string categoryName, bool isTimeRestricted, decimal minimumPassingScore)
    {
        EnsureEditable();

        if (string.IsNullOrWhiteSpace(name))
            throw new ExamDomainException("Exam name is required.");

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Exam category is required.");

        if (minimumPassingScore < 0 || minimumPassingScore > 10)
            throw new ExamDomainException("Minimum passing score must be between 0 and 10.");
```

- [ ] **Step 2: `ExamResult.Finish` nhận `decimal`**

Trong `src/Services/Exam/Exam.Domain/AggregateModels/ExamResultAggregate/ExamResult.cs`, dòng 141:
```csharp
    public void Finish(int minimumPassingScore)
```
Thay bằng:
```csharp
    public void Finish(decimal minimumPassingScore)
```

- [ ] **Step 3: Grading truyền decimal**

Trong `src/Services/Exam/Exam.Domain/Services/ExamResultGradingService.cs`, dòng 45:
```csharp
        examResult.Finish(exam?.MinimumPassingScore ?? 0);
```
Thay bằng:
```csharp
        examResult.Finish(exam?.MinimumPassingScore ?? 0m);
```

- [ ] **Step 4: Đổi kiểu trong contracts**

Trong `src/Services/Exam/Exam.Contracts/ExamDto.cs`, dòng 12:
```csharp
    int MinimumPassingScore,
```
Thay bằng:
```csharp
    decimal MinimumPassingScore,
```

Trong `src/Services/Exam/Exam.Contracts/ExamRequest.cs`, dòng 11:
```csharp
    int MinimumPassingScore);
```
Thay bằng:
```csharp
    decimal MinimumPassingScore);
```

- [ ] **Step 5: Đổi kiểu trong Create/Update command + validator**

Trong `.../Commands/CreateExam/CreateExamCommand.cs`, dòng 14:
```csharp
    int MinimumPassingScore,
```
Thay bằng:
```csharp
    decimal MinimumPassingScore,
```

Trong `.../Commands/CreateExam/CreateExamCommandValidator.cs`, dòng 13:
```csharp
        RuleFor(x => x.MinimumPassingScore).GreaterThanOrEqualTo(0);
```
Thay bằng:
```csharp
        RuleFor(x => x.MinimumPassingScore).InclusiveBetween(0m, 10m);
```

Trong `.../Commands/UpdateExam/UpdateExamCommand.cs`, dòng 15:
```csharp
    int MinimumPassingScore,
```
Thay bằng:
```csharp
    decimal MinimumPassingScore,
```

Trong `.../Commands/UpdateExam/UpdateExamCommandValidator.cs`, dòng 14:
```csharp
        RuleFor(x => x.MinimumPassingScore).GreaterThanOrEqualTo(0);
```
Thay bằng:
```csharp
        RuleFor(x => x.MinimumPassingScore).InclusiveBetween(0m, 10m);
```

- [ ] **Step 6: DecimalSerializer cho `Exam.MinimumPassingScore`**

Trong `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/MongoClassMaps.cs`, khối đăng ký `ExamEntity` (dòng 111-120), thay:
```csharp
                cm.MapCreator(e => new ExamEntity(e.Name, e.ShortDesc, e.Content, e.Duration, e.Level,
                    e.OwnerUserId, e.CategoryId, e.CategoryName, e.IsTimeRestricted, e.MinimumPassingScore));
                cm.MapMember(e => e.NegativeMarkingRatio).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
```
Bằng:
```csharp
                cm.MapCreator(e => new ExamEntity(e.Name, e.ShortDesc, e.Content, e.Duration, e.Level,
                    e.OwnerUserId, e.CategoryId, e.CategoryName, e.IsTimeRestricted, e.MinimumPassingScore));
                cm.MapMember(e => e.MinimumPassingScore).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
                cm.MapMember(e => e.NegativeMarkingRatio).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
```
(Lưu ý: dòng `NegativeMarkingRatio` sẽ bị xoá ở Task 3 khi field đó biến mất — ở Task 1 vẫn giữ để build xanh.)

- [ ] **Step 7: DataSeeder truyền decimal**

Trong `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/DataSeeder.cs`, dòng 96:
```csharp
                isTimeRestricted: true, minimumPassingScore: (int)Math.Ceiling(questionIds.Count * 0.6));
```
Thay bằng:
```csharp
                isTimeRestricted: true, minimumPassingScore: 6.0m);
```
Dòng 117:
```csharp
                isTimeRestricted: false, minimumPassingScore: (int)Math.Ceiling(Math.Min(questionIds.Count, 2) * 0.6));
```
Thay bằng:
```csharp
                isTimeRestricted: false, minimumPassingScore: 6.0m);
```

- [ ] **Step 8: Ô nhập UI đổi sang decimal 0–10**

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/ExamFormDialog.razor`, dòng 18:
```razor
        <AppNumericField TValue="int" Label="Điểm đạt tối thiểu" @bind-Value="minimumPassingScore" Min="0" Required="true" />
```
Thay bằng:
```razor
        <AppNumericField TValue="decimal" Label="Điểm đạt tối thiểu (0–10)" @bind-Value="minimumPassingScore" Min="0" Max="10" Step="0.5" Required="true" />
```

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/ExamFormDialog.razor.cs`, dòng 19:
```csharp
    private int minimumPassingScore = 5;
```
Thay bằng:
```csharp
    private decimal minimumPassingScore = 5m;
```
(`minimumPassingScore = Model.MinimumPassingScore;` ở dòng 50 tự khớp vì `Model.MinimumPassingScore` giờ là `decimal`.)

- [ ] **Step 9: Build toàn solution**

Run: `dotnet build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 10: Commit**

```bash
git add src/Services/Exam/Exam.Domain/AggregateModels/ExamAggregate/Exam.cs src/Services/Exam/Exam.Domain/AggregateModels/ExamResultAggregate/ExamResult.cs src/Services/Exam/Exam.Domain/Services/ExamResultGradingService.cs src/Services/Exam/Exam.Contracts/ExamDto.cs src/Services/Exam/Exam.Contracts/ExamRequest.cs src/Services/Exam/Exam.Application/ExamAggregate/Commands/CreateExam/CreateExamCommand.cs src/Services/Exam/Exam.Application/ExamAggregate/Commands/CreateExam/CreateExamCommandValidator.cs src/Services/Exam/Exam.Application/ExamAggregate/Commands/UpdateExam/UpdateExamCommand.cs src/Services/Exam/Exam.Application/ExamAggregate/Commands/UpdateExam/UpdateExamCommandValidator.cs src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/MongoClassMaps.cs src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/DataSeeder.cs src/Web/Exam.WebApp/Components/Pages/Admin/ExamFormDialog.razor src/Web/Exam.WebApp/Components/Pages/Admin/ExamFormDialog.razor.cs
git commit -m "feat: chuyển điểm đạt tối thiểu sang thang thập phân 0–10"
```

---

## Task 2: Bỏ field `Points`, chấm điểm theo thang 10 (đếm câu đúng), bỏ trừ điểm khi chấm

**Files:**
- Modify: `src/Services/Exam/Exam.Domain/AggregateModels/QuestionAggregate/Question.cs` (xoá `Points`, `ChangePoints`, tham số `points`)
- Modify: `src/Services/Exam/Exam.Domain/AggregateModels/ExamResultAggregate/QuestionResult.cs` (xoá `Points`)
- Modify: `src/Services/Exam/Exam.Domain/AggregateModels/ExamResultAggregate/ExamResult.cs` (xoá `NegativeMarkingRatio`/`ScoreFor`, đổi `TotalScore`/`MaxPossibleScore`)
- Modify: `src/Services/Exam/Exam.Domain/Services/ExamResultGradingService.cs` (bỏ `question.Points`)
- Modify: `src/Services/Exam/Exam.Application/ExamResultAggregate/Commands/StartExam/StartExamCommandHandler.cs` (bỏ tham số ratio khi `new ExamResult`)
- Modify: `src/Services/Exam/Exam.Application/ExamResultAggregate/ExamResultMapper.cs` (bỏ `q.Points`/`qr.Points`)
- Modify: `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/MongoClassMaps.cs` (creator của `Question`, `QuestionResult`, `ExamResult`)
- Modify contracts: `QuestionDto.cs`, `QuestionRequest.cs`, `ExamAttemptDto.cs`, `ExamResultDto.cs`, `ExamResultSummaryDto.cs`, `ExamResultAdminListItemDto.cs`, `ImportQuestionsResultDto.cs`
- Modify: `CreateQuestionCommand.cs`/`Handler`/`Validator`, `UpdateQuestionCommand.cs`/`Handler`/`Validator`, `QuestionMapper.cs`, `QuestionsController.cs`, `ImportQuestionsCommandHandler.cs`, `ExportQuestionsQueryHandler.cs`, `DataSeeder.cs`
- Modify WebApp: `QuestionFormDialog.razor`/`.razor.cs`, `Questions.razor`, `ImportQuestionsDialog.razor`, `TakeExam.razor`

**Interfaces:**
- Consumes: `Exam.MinimumPassingScore : decimal`, `ExamResult.Finish(decimal)` (Task 1).
- Produces: `Question` ctor không còn `points`; `QuestionResult` ctor không còn `points`; `ExamResult.TotalScore` = thang 10; `ExamResult.MaxPossibleScore : decimal (= 10m)`; `ExamResult(string userId, string examId)`. Task 3 dựa trên `ExamResult(userId, examId)` (không ratio).

- [ ] **Step 1: Thay toàn bộ `Question.cs`**

Thay toàn bộ `src/Services/Exam/Exam.Domain/AggregateModels/QuestionAggregate/Question.cs` bằng:
```csharp
using Exam.Contracts;
using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.QuestionAggregate;

public class Question : Entity, IAggregateRoot
{
    public string Content { get; private set; }

    public QuestionType QuestionType { get; private set; }

    public Level Level { get; private set; }

    public string CategoryId { get; private set; }

    public string CategoryName { get; private set; }

    public IReadOnlyCollection<Answer> Answers { get; private set; }

    public string Explain { get; private set; }

    public DateTime DateCreated { get; private set; }

    public string OwnerUserId { get; private set; }

    private Question()
    {
    }

    public Question(string id, string content, QuestionType questionType, Level level, string categoryId,
        IReadOnlyCollection<Answer> answers, string explain, string ownerUserId = null, string categoryName = null)
    {
        EnsureValid(content, categoryId, answers, questionType);

        Id = id;
        Content = content;
        QuestionType = questionType;
        Level = level;
        CategoryId = categoryId;
        Answers = answers;
        Explain = explain;
        DateCreated = DateTime.UtcNow;
        OwnerUserId = ownerUserId;
        CategoryName = categoryName;
    }

    public void Update(string content, QuestionType questionType, Level level, string categoryId, string categoryName,
        IReadOnlyCollection<Answer> answers, string explain)
    {
        EnsureValid(content, categoryId, answers, questionType);

        Content = content;
        QuestionType = questionType;
        Level = level;
        CategoryId = categoryId;
        CategoryName = categoryName;
        Answers = answers;
        Explain = explain;
    }

    public void ChangeCategory(string categoryId, string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Question category is required.");

        CategoryId = categoryId;
        CategoryName = categoryName;
    }

    private static void EnsureValid(string content, string categoryId, IReadOnlyCollection<Answer> answers,
        QuestionType questionType)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ExamDomainException("Question content is required.");

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Question category is required.");

        if (answers == null || answers.Count == 0)
            throw new ExamDomainException($"{nameof(answers)} can not be empty.");

        if (questionType == QuestionType.SingleSelection && answers.Count(x => x.IsCorrect) > 1)
            throw new ExamDomainException($"{nameof(answers)} is invalid: a single selection question can only have one correct answer.");
    }
}
```

- [ ] **Step 2: Thay toàn bộ `QuestionResult.cs`**

Thay toàn bộ `src/Services/Exam/Exam.Domain/AggregateModels/ExamResultAggregate/QuestionResult.cs` bằng:
```csharp
using Exam.Contracts;
using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamResultAggregate;

public class QuestionResult : Entity
{
    public string Content { get; private set; }

    public QuestionType QuestionType { get; private set; }

    public Level Level { get; private set; }

    public string Explain { get; private set; }

    public IReadOnlyCollection<AnswerResult> Answers { get; private set; }

    public bool Result { get; private set; }

    public bool IsAnswered => Answers.Any(a => a.UserChosen == true);

    private QuestionResult()
    {
    }

    public QuestionResult(string id, string content, QuestionType questionType, Level level,
        IReadOnlyCollection<AnswerResult> answers, string explain)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ExamDomainException("Question content is required.");

        if (answers == null || answers.Count == 0)
            throw new ExamDomainException("Question result must have at least one answer.");

        Id = id;
        Content = content;
        QuestionType = questionType;
        Level = level;
        Explain = explain;
        Answers = answers;
        Result = answers.All(a => a.IsCorrect == (a.UserChosen == true));
    }
}
```

- [ ] **Step 3: Thay toàn bộ `ExamResult.cs`**

Thay toàn bộ `src/Services/Exam/Exam.Domain/AggregateModels/ExamResultAggregate/ExamResult.cs` bằng:
```csharp
using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamResultAggregate;

public class ExamResult : Entity, IAggregateRoot
{
    private List<QuestionResult> _questionResults = new();
    private List<string> _questionIds = new();
    private List<DraftAnswer> _draftAnswers = new();

    public string ExamId { get; private set; }

    public string ExamTitle { get; private set; }

    public string UserId { get; private set; }

    public string Email { get; private set; }

    public string FullName { get; private set; }

    public IReadOnlyCollection<string> QuestionIds
    {
        get => _questionIds;
        private set => _questionIds = value?.ToList() ?? new List<string>();
    }

    public IReadOnlyCollection<QuestionResult> QuestionResults
    {
        get => _questionResults;
        private set => _questionResults = value?.ToList() ?? new List<QuestionResult>();
    }

    public IReadOnlyCollection<DraftAnswer> DraftAnswers
    {
        get => _draftAnswers;
        private set => _draftAnswers = value?.ToList() ?? new List<DraftAnswer>();
    }

    public TimeSpan? Duration { get; private set; }

    public DateTime? Deadline => Duration.HasValue ? ExamStartDate.Add(Duration.Value) : null;

    public int CorrectQuestionCount { get; private set; }

    // Thang điểm 10 chuẩn học vụ Việt Nam: (số câu đúng / tổng số câu) × 10, làm tròn 2 chữ số thập phân.
    // Guard chia-cho-0 dù Publish() đã chặn đề rỗng - tránh ném DivideByZeroException khó hiểu.
    public decimal TotalScore => _questionResults.Count == 0
        ? 0m
        : Math.Round((decimal)CorrectQuestionCount / _questionResults.Count * 10m, 2);

    // Hằng số thang điểm - giữ làm property tiện lợi để binding "@TotalScore/@MaxPossibleScore điểm" không cần sửa.
    public decimal MaxPossibleScore => 10m;

    public DateTime ExamStartDate { get; private set; }

    public DateTime? ExamFinishDate { get; private set; }

    public bool? Passed { get; private set; }

    public bool Finished { get; private set; }

    private ExamResult()
    {
    }

    public ExamResult(string userId, string examId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ExamDomainException("UserId is required.");

        if (string.IsNullOrWhiteSpace(examId))
            throw new ExamDomainException("ExamId is required.");

        UserId = userId;
        ExamId = examId;
        ExamStartDate = DateTime.UtcNow;
        Finished = false;
    }

    public void SetExamTitle(string examTitle)
    {
        if (string.IsNullOrWhiteSpace(examTitle))
            throw new ExamDomainException("Exam title is required.");

        ExamTitle = examTitle;
    }

    public void SetUserInfo(string email, string fullName)
    {
        Email = email;
        FullName = fullName;
    }

    public void SetDuration(TimeSpan? duration)
    {
        Duration = duration;
    }

    public bool IsExpired(DateTime at) => !Finished && Deadline.HasValue && at >= Deadline.Value;

    public void RecordAnswer(string questionId, IEnumerable<string> selectedAnswerIds)
    {
        if (Finished)
            throw new ExamDomainException("Cannot record an answer after the exam has finished.");

        if (!_questionIds.Contains(questionId))
            throw new ExamDomainException("This question is not part of the current exam attempt.");

        var ids = selectedAnswerIds?.ToList() ?? new List<string>();

        _draftAnswers.RemoveAll(d => d.QuestionId == questionId);
        _draftAnswers.Add(new DraftAnswer(questionId, ids));
    }

    public void AssignQuestions(IEnumerable<string> questionIds)
    {
        if (Finished)
            throw new ExamDomainException("Cannot assign questions after the exam has finished.");

        if (_questionIds.Count > 0)
            throw new ExamDomainException("Questions have already been assigned to this attempt.");

        var ids = questionIds?.ToList() ?? new List<string>();

        if (ids.Count == 0)
            throw new ExamDomainException("At least one question must be assigned to this attempt.");

        _questionIds = ids;
    }

    public void AddQuestionResult(QuestionResult questionResult)
    {
        if (Finished)
            throw new ExamDomainException("Cannot add a question result after the exam has finished.");

        _questionResults.Add(questionResult);
    }

    public void Finish(decimal minimumPassingScore)
    {
        if (Finished)
            throw new ExamDomainException("Exam result is already finished.");

        CorrectQuestionCount = _questionResults.Count(x => x.Result);
        Passed = TotalScore >= minimumPassingScore;
        ExamFinishDate = DateTime.UtcNow;
        Finished = true;
    }
}
```

- [ ] **Step 4: Grading không dùng `Points`**

Trong `src/Services/Exam/Exam.Domain/Services/ExamResultGradingService.cs`, dòng 41-42:
```csharp
            examResult.AddQuestionResult(new QuestionResult(question.Id, question.Content, question.QuestionType,
                question.Level, answerResults, question.Explain, question.Points));
```
Thay bằng:
```csharp
            examResult.AddQuestionResult(new QuestionResult(question.Id, question.Content, question.QuestionType,
                question.Level, answerResults, question.Explain));
```

- [ ] **Step 5: `StartExamCommandHandler` không truyền ratio**

Trong `src/Services/Exam/Exam.Application/ExamResultAggregate/Commands/StartExam/StartExamCommandHandler.cs`, dòng 76:
```csharp
        var examResult = new ExamResult(request.UserId, request.ExamId, exam.NegativeMarkingRatio);
```
Thay bằng:
```csharp
        var examResult = new ExamResult(request.UserId, request.ExamId);
```

- [ ] **Step 6: `ExamResultMapper` không dùng `Points`**

Trong `src/Services/Exam/Exam.Application/ExamResultAggregate/ExamResultMapper.cs`, khối `ToAttemptDto` dòng 19-27 — xoá dòng `q.Points,`:
```csharp
            .Select(q => new ExamAttemptQuestionDto(
                q.Id,
                q.Content,
                q.QuestionType,
                q.Level,
                q.Points,
                DeterministicShuffle(q.Answers, examResult.Id + q.Id)
```
Thành:
```csharp
            .Select(q => new ExamAttemptQuestionDto(
                q.Id,
                q.Content,
                q.QuestionType,
                q.Level,
                DeterministicShuffle(q.Answers, examResult.Id + q.Id)
```
Trong `ToResultDto` dòng 53-61 — xoá dòng `qr.Points,`:
```csharp
            examResult.QuestionResults.Select(qr => new QuestionResultDto(
                qr.Id,
                qr.Content,
                qr.QuestionType,
                qr.Level,
                qr.Explain,
                qr.Points,
                qr.Result,
                qr.IsAnswered,
```
Thành:
```csharp
            examResult.QuestionResults.Select(qr => new QuestionResultDto(
                qr.Id,
                qr.Content,
                qr.QuestionType,
                qr.Level,
                qr.Explain,
                qr.Result,
                qr.IsAnswered,
```

- [ ] **Step 7: MongoClassMaps — bỏ `Points`/`NegativeMarkingRatio` khỏi 3 creator**

Trong `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/MongoClassMaps.cs`:

Creator `Question` (dòng 87-88):
```csharp
                cm.MapCreator(q => new Question(q.Id, q.Content, q.QuestionType, q.Level, q.CategoryId,
                    q.Answers, q.Explain, q.Points, q.OwnerUserId, q.CategoryName));
```
Thành:
```csharp
                cm.MapCreator(q => new Question(q.Id, q.Content, q.QuestionType, q.Level, q.CategoryId,
                    q.Answers, q.Explain, q.OwnerUserId, q.CategoryName));
```

Creator `QuestionResult` (dòng 106-107):
```csharp
                cm.MapCreator(q => new QuestionResult(q.Id, q.Content, q.QuestionType, q.Level,
                    q.Answers, q.Explain, q.Points));
```
Thành:
```csharp
                cm.MapCreator(q => new QuestionResult(q.Id, q.Content, q.QuestionType, q.Level,
                    q.Answers, q.Explain));
```

Creator `ExamResult` (dòng 129-134):
```csharp
            BsonClassMap.RegisterClassMap<ExamResult>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(r => new ExamResult(r.UserId, r.ExamId, r.NegativeMarkingRatio));
                cm.MapMember(r => r.NegativeMarkingRatio).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            });
```
Thành:
```csharp
            BsonClassMap.RegisterClassMap<ExamResult>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(r => new ExamResult(r.UserId, r.ExamId));
            });
```

- [ ] **Step 8: Bỏ `Points` khỏi contracts**

`src/Services/Exam/Exam.Contracts/QuestionDto.cs` — xoá dòng 12 `int Points,`:
```csharp
public record QuestionDto(
    string Id,
    string Content,
    QuestionType QuestionType,
    Level Level,
    string CategoryId,
    string CategoryName,
    IReadOnlyCollection<AnswerDto> Answers,
    string Explain,
    string OwnerUserId);
```

`src/Services/Exam/Exam.Contracts/QuestionRequest.cs` — xoá dòng 10 `int Points`:
```csharp
public record QuestionRequest(
    string Content,
    QuestionType QuestionType,
    Level Level,
    string CategoryId,
    IReadOnlyCollection<AnswerInput> Answers,
    string Explain);
```

`src/Services/Exam/Exam.Contracts/ExamAttemptDto.cs` — `ExamAttemptQuestionDto` xoá `int Points,` (dòng 17):
```csharp
public record ExamAttemptQuestionDto(
    string Id,
    string Content,
    QuestionType QuestionType,
    Level Level,
    IReadOnlyCollection<ExamAttemptAnswerOptionDto> Answers);
```

`src/Services/Exam/Exam.Contracts/ExamResultDto.cs` — `ExamResultDto.MaxPossibleScore` `int`→`decimal` (dòng 11), và `QuestionResultDto` xoá `int Points,` (dòng 25):
```csharp
public record ExamResultDto(
    string Id,
    string ExamId,
    string ExamTitle,
    string UserId,
    string Email,
    string FullName,
    decimal TotalScore,
    decimal MaxPossibleScore,
    int CorrectQuestionCount,
    bool? Passed,
    DateTime ExamStartDate,
    DateTime? ExamFinishDate,
    bool Finished,
    IReadOnlyCollection<QuestionResultDto> QuestionResults);

public record QuestionResultDto(
    string Id,
    string Content,
    QuestionType QuestionType,
    Level Level,
    string Explain,
    bool Result,
    bool IsAnswered,
    IReadOnlyCollection<AnswerResultDto> Answers);

public record AnswerResultDto(string Id, string Content, bool? UserChosen, bool IsCorrect);
```

`src/Services/Exam/Exam.Contracts/ExamResultSummaryDto.cs` — dòng 8 `int MaxPossibleScore,`→`decimal MaxPossibleScore,`.

`src/Services/Exam/Exam.Contracts/ExamResultAdminListItemDto.cs` — dòng 9 `int MaxPossibleScore,`→`decimal MaxPossibleScore,`.

`src/Services/Exam/Exam.Contracts/ImportQuestionsResultDto.cs` — `ImportQuestionPreviewRow` xoá `, int Points` (dòng 11):
```csharp
public record ImportQuestionPreviewRow(int RowNumber, string CategoryName, string Content, string QuestionType, string Level);
```

- [ ] **Step 9: Bỏ `Points` khỏi Question command/handler/validator/mapper/controller**

`.../CreateQuestion/CreateQuestionCommand.cs` — xoá dòng 12 `int Points,`:
```csharp
public record CreateQuestionCommand(
    string Content,
    QuestionType QuestionType,
    Level Level,
    string CategoryId,
    IReadOnlyCollection<AnswerInput> Answers,
    string Explain,
    string OwnerUserId) : IRequest<QuestionDto>;
```

`.../CreateQuestion/CreateQuestionCommandHandler.cs` — dòng 29-30:
```csharp
        var question = new Question(null, request.Content, request.QuestionType, request.Level, category.Id,
            answers, request.Explain, request.Points, request.OwnerUserId, category.Name);
```
Thành:
```csharp
        var question = new Question(null, request.Content, request.QuestionType, request.Level, category.Id,
            answers, request.Explain, request.OwnerUserId, category.Name);
```

`.../CreateQuestion/CreateQuestionCommandValidator.cs` — xoá dòng 13 `RuleFor(x => x.Points).GreaterThan(0);`.

`.../UpdateQuestion/UpdateQuestionCommand.cs` — xoá dòng 15 `int Points,`:
```csharp
public record UpdateQuestionCommand(
    string Id,
    string Content,
    QuestionType QuestionType,
    Level Level,
    string CategoryId,
    IReadOnlyCollection<AnswerInput> Answers,
    string Explain,
    Actor Actor) : IRequest<QuestionDto>;
```

`.../UpdateQuestion/UpdateQuestionCommandHandler.cs` — dòng 34-35:
```csharp
        question.Update(request.Content, request.QuestionType, request.Level, category.Id, category.Name,
            answers, request.Explain, request.Points);
```
Thành:
```csharp
        question.Update(request.Content, request.QuestionType, request.Level, category.Id, category.Name,
            answers, request.Explain);
```

`.../UpdateQuestion/UpdateQuestionCommandValidator.cs` — xoá dòng 14 `RuleFor(x => x.Points).GreaterThan(0);`.

`src/Services/Exam/Exam.Application/QuestionAggregate/QuestionMapper.cs` — xoá dòng 15 `question.Points,`:
```csharp
    public static QuestionDto ToDto(Domain.AggregateModels.QuestionAggregate.Question question) =>
        new(
            question.Id,
            question.Content,
            question.QuestionType,
            question.Level,
            question.CategoryId,
            question.CategoryName,
            question.Answers.Select(a => new AnswerDto(a.Id, a.Content, a.IsCorrect)).ToList(),
            question.Explain,
            question.OwnerUserId);
```

`src/Services/Exam/Exam.API/Controllers/QuestionsController.cs` — dòng 33-34 (Create):
```csharp
        var command = new CreateQuestionCommand(request.Content, request.QuestionType, request.Level,
            request.CategoryId, request.Answers, request.Explain, request.Points, User.GetUserId()!);
```
Thành:
```csharp
        var command = new CreateQuestionCommand(request.Content, request.QuestionType, request.Level,
            request.CategoryId, request.Answers, request.Explain, User.GetUserId()!);
```
Dòng 70-71 (Update):
```csharp
        var command = new UpdateQuestionCommand(id, request.Content, request.QuestionType, request.Level,
            request.CategoryId, request.Answers, request.Explain, request.Points, User.GetActor());
```
Thành:
```csharp
        var command = new UpdateQuestionCommand(id, request.Content, request.QuestionType, request.Level,
            request.CategoryId, request.Answers, request.Explain, User.GetActor());
```

- [ ] **Step 10: Bỏ cột Điểm khỏi Excel import/export**

`src/Services/Exam/Exam.Application/QuestionAggregate/Commands/ImportQuestions/ImportQuestionsCommandHandler.cs`:

Xoá dòng 51 (`var pointsText = row.Cell(11).GetString().Trim();`).

Xoá khối dòng 102-104:
```csharp
            var points = 1;
            if (!string.IsNullOrWhiteSpace(pointsText) && (!int.TryParse(pointsText, out points) || points <= 0))
                rowErrors.Add("Điểm không hợp lệ (phải là số nguyên dương).");
```

Dòng 116-118:
```csharp
            questionsToInsert.Add(new Question(null!, content, questionType!.Value, level!.Value, category!.Id,
                answers, explain, points, request.OwnerUserId, category.Name));
            validRows.Add(new ImportQuestionPreviewRow(rowNumber, category.Name, content, questionTypeText, levelText, points));
```
Thành:
```csharp
            questionsToInsert.Add(new Question(null!, content, questionType!.Value, level!.Value, category!.Id,
                answers, explain, request.OwnerUserId, category.Name));
            validRows.Add(new ImportQuestionPreviewRow(rowNumber, category.Name, content, questionTypeText, levelText));
```

`src/Services/Exam/Exam.Application/QuestionAggregate/Queries/ExportQuestions/ExportQuestionsQueryHandler.cs`:

Dòng 9-13 `Headers` — bỏ `"Điểm"`:
```csharp
    private static readonly string[] Headers =
    [
        "Môn học", "Mức độ", "Loại câu hỏi", "Nội dung câu hỏi",
        "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D", "Đáp án đúng", "Giải thích"
    ];
```
Xoá dòng 54 `worksheet.Cell(rowNumber, 11).Value = question.Points;`.

- [ ] **Step 11: DataSeeder — Question không còn `points`**

Trong `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/DataSeeder.cs`, dòng 79-81:
```csharp
                var question = new Question(null, seedQuestion.Content, seedQuestion.Type, seedQuestion.Level,
                    category.Id, answers, seedQuestion.Explain, points: 1, ownerUserId: instructor.ExternalId,
                    categoryName: category.Name);
```
Thành:
```csharp
                var question = new Question(null, seedQuestion.Content, seedQuestion.Type, seedQuestion.Level,
                    category.Id, answers, seedQuestion.Explain, ownerUserId: instructor.ExternalId,
                    categoryName: category.Name);
```

- [ ] **Step 12: Bỏ `Points` khỏi UI câu hỏi**

`src/Web/Exam.WebApp/Components/Pages/Admin/QuestionFormDialog.razor` — xoá khối dòng 18-20:
```razor
    <div style="max-width:160px;">
        <AppNumericField TValue="int" Label="Điểm" @bind-Value="points" Min="1" />
    </div>

```

`src/Web/Exam.WebApp/Components/Pages/Admin/QuestionFormDialog.razor.cs`:
- Xoá dòng 17 `private int points = 1;`.
- Xoá dòng 30 `points = Model.Points;`.
- Trong `Submit`, khối `new QuestionRequest(...)` (dòng 84-91) bỏ `points`:
```csharp
        var request = new QuestionRequest(
            content,
            questionType,
            level,
            CategoryId,
            nonEmptyAnswers.Select(a => new AnswerInput(a.Content, a.IsCorrect)).ToList(),
            explain);
```

`src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor`:
- Dòng 55 header — bỏ `<th>Điểm</th>`:
```razor
                <th style="width:32px;"></th><th>Nội dung</th><th>Loại</th><th>Mức độ</th><th>Đáp án</th><th></th>
```
- Xoá dòng 67 `<td>@question.Points</td>`.

`src/Web/Exam.WebApp/Components/Pages/Admin/ImportQuestionsDialog.razor`:
- Dòng 27 header — bỏ `<th>Điểm</th>`:
```razor
                <thead><tr><th>Dòng</th><th>Môn học</th><th>Nội dung</th><th>Loại</th></tr></thead>
```
- Xoá dòng 36 `<td>@row.Points</td>`.

`src/Web/Exam.WebApp/Components/Pages/TakeExam.razor` — xoá dòng 34 `<span class="q-points">@question.Points điểm</span>`.

- [ ] **Step 13: Build toàn solution**

Run: `dotnet build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 14: Commit**

```bash
git add src/Services/Exam/Exam.Domain/AggregateModels/QuestionAggregate/Question.cs src/Services/Exam/Exam.Domain/AggregateModels/ExamResultAggregate/QuestionResult.cs src/Services/Exam/Exam.Domain/AggregateModels/ExamResultAggregate/ExamResult.cs src/Services/Exam/Exam.Domain/Services/ExamResultGradingService.cs src/Services/Exam/Exam.Application/ExamResultAggregate/Commands/StartExam/StartExamCommandHandler.cs src/Services/Exam/Exam.Application/ExamResultAggregate/ExamResultMapper.cs src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/MongoClassMaps.cs src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/DataSeeder.cs src/Services/Exam/Exam.Contracts/QuestionDto.cs src/Services/Exam/Exam.Contracts/QuestionRequest.cs src/Services/Exam/Exam.Contracts/ExamAttemptDto.cs src/Services/Exam/Exam.Contracts/ExamResultDto.cs src/Services/Exam/Exam.Contracts/ExamResultSummaryDto.cs src/Services/Exam/Exam.Contracts/ExamResultAdminListItemDto.cs src/Services/Exam/Exam.Contracts/ImportQuestionsResultDto.cs src/Services/Exam/Exam.Application/QuestionAggregate src/Services/Exam/Exam.API/Controllers/QuestionsController.cs src/Web/Exam.WebApp/Components/Pages/Admin/QuestionFormDialog.razor src/Web/Exam.WebApp/Components/Pages/Admin/QuestionFormDialog.razor.cs src/Web/Exam.WebApp/Components/Pages/Admin/Questions.razor src/Web/Exam.WebApp/Components/Pages/Admin/ImportQuestionsDialog.razor src/Web/Exam.WebApp/Components/Pages/TakeExam.razor
git commit -m "refactor: bỏ field Points, chấm điểm theo thang 10 và bỏ trừ điểm khi chấm"
```

---

## Task 3: Ma trận cấu trúc đề thi thay Fixed/Pool, bỏ negative marking, dọn permission

> **Đây là task lớn, thay đổi phá vỡ tương thích** — thay đổi entity `Exam` khiến toàn solution phải cập nhật đồng thời mới build xanh, nên chỉ có 1 lần build + 1 commit ở cuối. Làm tuần tự các step; chỉ chạy build ở Step 16.

**Files:**
- Create: `src/Services/Exam/Exam.Domain/AggregateModels/ExamAggregate/ExamCompositionCell.cs`
- Create: `src/Services/Exam/Exam.Application/ExamAggregate/Commands/ConfigureExamComposition/ConfigureExamCompositionCommand.cs` + `Handler.cs` + `Validator.cs`
- Delete: thư mục `.../Commands/ConfigureQuestionPool/`, `.../Commands/AddQuestionToExam/`, `.../Commands/RemoveQuestionFromExam/`, `.../Commands/ConfigureNegativeMarking/`
- Delete: `src/Services/Exam/Exam.Contracts/Enums/QuestionSelectionMode.cs`
- Modify: `Exam.cs` (rewrite), `ExamQuestionPoolService.cs` (rewrite), `IQuestionRepository.cs` + `QuestionRepository.cs` (thêm method lọc theo Level+Type), `ExamsController.cs`, `ExamDto.cs`, `ExamRequest.cs`, `ExamMapper.cs`, `MongoClassMaps.cs`, `StartExamCommandHandler.cs`, `DataSeeder.cs`, `Permissions.cs`, `Permissions.razor.cs`
- Modify WebApp: `ApiRoutes.cs`, `ExamApiClient.cs`, `ExamFormResult.cs`, `ExamFormDialog.razor` + `.razor.cs`, `Exams.razor` + `.razor.cs`

**Interfaces:**
- Consumes: `ExamResult(userId, examId)` (Task 2), `Exam.MinimumPassingScore : decimal` (Task 1).
- Produces: `ExamCompositionCell(Level, QuestionType, int)`, `Exam.Composition : IReadOnlyCollection<ExamCompositionCell>`, `Exam.ConfigureComposition(IReadOnlyCollection<ExamCompositionCell>)`, `ExamCompositionCellDto(Level, QuestionType, int Count)`, `ConfigureExamCompositionRequest(IReadOnlyCollection<ExamCompositionCellDto>)`, `IQuestionRepository.GetByCategoryLevelTypeAsync(string, Level, QuestionType, CancellationToken)`.

- [ ] **Step 1: Tạo value object `ExamCompositionCell`**

Tạo `src/Services/Exam/Exam.Domain/AggregateModels/ExamAggregate/ExamCompositionCell.cs`:
```csharp
using Exam.Contracts;
using Exam.Domain.Exceptions;

namespace Exam.Domain.AggregateModels.ExamAggregate;

// Một ô trong ma trận cấu trúc đề thi: rút ngẫu nhiên đúng Count câu hỏi thuộc mức độ Level + loại
// QuestionType (trong đúng môn học của đề). Count có thể bằng 0 (ô trống); ràng buộc tổng > 0 nằm ở
// Exam.ConfigureComposition.
public class ExamCompositionCell
{
    public Level Level { get; private set; }

    public QuestionType QuestionType { get; private set; }

    public int Count { get; private set; }

    private ExamCompositionCell()
    {
    }

    public ExamCompositionCell(Level level, QuestionType questionType, int count)
    {
        if (count < 0)
            throw new ExamDomainException("Composition cell count must not be negative.");

        Level = level;
        QuestionType = questionType;
        Count = count;
    }
}
```

- [ ] **Step 2: Thay toàn bộ `Exam.cs`**

Thay toàn bộ `src/Services/Exam/Exam.Domain/AggregateModels/ExamAggregate/Exam.cs` bằng (đã gồm cả `MinimumPassingScore : decimal` từ Task 1):
```csharp
using Exam.Contracts;
using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamAggregate;

public class Exam : Entity, IAggregateRoot
{
    private List<ExamCompositionCell> _composition = new();
    private List<string> _assignedClassIds = new();

    public string Name { get; private set; }

    public string ShortDesc { get; private set; }

    public string Content { get; private set; }

    public TimeSpan Duration { get; private set; }

    public Level Level { get; private set; }

    public DateTime DateCreated { get; private set; }

    public string OwnerUserId { get; private set; }

    public decimal MinimumPassingScore { get; private set; }

    public bool IsTimeRestricted { get; private set; }

    public string CategoryId { get; private set; }

    public string CategoryName { get; private set; }

    public ExamStatus Status { get; private set; }

    public DateTime? AvailableFrom { get; private set; }

    public DateTime? AvailableTo { get; private set; }

    // null = không giới hạn số lần thi lại.
    public int? MaxAttempts { get; private set; }

    // Ma trận cấu trúc đề thi: hệ thống tự rút ngẫu nhiên theo từng ô khi học viên bắt đầu làm bài.
    public IReadOnlyCollection<ExamCompositionCell> Composition
    {
        get => _composition;
        private set => _composition = value?.ToList() ?? new List<ExamCompositionCell>();
    }

    public IReadOnlyCollection<string> AssignedClassIds
    {
        get => _assignedClassIds;
        private set => _assignedClassIds = value?.ToList() ?? new List<string>();
    }

    // Đề không giao cho lớp nào là đề công khai - mọi user đăng nhập đều thi được.
    public bool IsPublic => _assignedClassIds.Count == 0;

    public int NumberOfQuestions => _composition.Sum(c => c.Count);

    private Exam()
    {
    }

    public Exam(string name, string shortDesc, string content, TimeSpan duration, Level level, string ownerUserId,
        string categoryId, string categoryName, bool isTimeRestricted, decimal minimumPassingScore)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ExamDomainException("Exam name is required.");

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Exam category is required.");

        if (minimumPassingScore < 0 || minimumPassingScore > 10)
            throw new ExamDomainException("Minimum passing score must be between 0 and 10.");

        Name = name;
        ShortDesc = shortDesc;
        Content = content;
        Duration = duration;
        Level = level;
        OwnerUserId = ownerUserId;
        CategoryId = categoryId;
        CategoryName = categoryName;
        IsTimeRestricted = isTimeRestricted;
        MinimumPassingScore = minimumPassingScore;
        DateCreated = DateTime.UtcNow;
        Status = ExamStatus.Draft;
    }

    public void UpdateDetails(string name, string shortDesc, string content, TimeSpan duration, Level level,
        string categoryId, string categoryName, bool isTimeRestricted, decimal minimumPassingScore)
    {
        EnsureEditable();

        if (string.IsNullOrWhiteSpace(name))
            throw new ExamDomainException("Exam name is required.");

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Exam category is required.");

        if (minimumPassingScore < 0 || minimumPassingScore > 10)
            throw new ExamDomainException("Minimum passing score must be between 0 and 10.");

        Name = name;
        ShortDesc = shortDesc;
        Content = content;
        Duration = duration;
        Level = level;
        CategoryId = categoryId;
        CategoryName = categoryName;
        IsTimeRestricted = isTimeRestricted;
        MinimumPassingScore = minimumPassingScore;
    }

    public void ConfigureComposition(IReadOnlyCollection<ExamCompositionCell> cells)
    {
        EnsureEditable();

        if (cells == null || cells.Count == 0)
            throw new ExamDomainException("Exam composition must have at least one cell.");

        if (cells.Any(c => c.Count < 0))
            throw new ExamDomainException("Composition cell count must not be negative.");

        if (cells.Sum(c => c.Count) <= 0)
            throw new ExamDomainException("Exam composition total question count must be greater than zero.");

        _composition = cells.ToList();
    }

    public void ScheduleAvailability(DateTime? availableFrom, DateTime? availableTo)
    {
        if (Status == ExamStatus.Archived)
            throw new ExamDomainException("Cannot change the availability window of an archived exam.");

        if (availableFrom.HasValue && availableTo.HasValue && availableTo <= availableFrom)
            throw new ExamDomainException("Available-to must be later than available-from.");

        AvailableFrom = availableFrom;
        AvailableTo = availableTo;
    }

    public bool IsAvailable(DateTime at)
    {
        if (Status != ExamStatus.Published)
            return false;

        if (AvailableFrom.HasValue && at < AvailableFrom.Value)
            return false;

        if (AvailableTo.HasValue && at > AvailableTo.Value)
            return false;

        return true;
    }

    public void ConfigureMaxAttempts(int? maxAttempts)
    {
        EnsureEditable();

        if (maxAttempts.HasValue && maxAttempts.Value <= 0)
            throw new ExamDomainException("Max attempts must be greater than zero.");

        MaxAttempts = maxAttempts;
    }

    public void Publish()
    {
        if (Status == ExamStatus.Archived)
            throw new ExamDomainException("An archived exam cannot be published.");

        if (Status == ExamStatus.Published)
            throw new ExamDomainException("Exam is already published.");

        if (NumberOfQuestions == 0)
            throw new ExamDomainException("Cannot publish an exam with no questions.");

        Status = ExamStatus.Published;
    }

    public void Unpublish()
    {
        if (Status != ExamStatus.Published)
            throw new ExamDomainException("Only a published exam can be unpublished.");

        Status = ExamStatus.Draft;
    }

    public void Archive()
    {
        if (Status == ExamStatus.Archived)
            throw new ExamDomainException("Exam is already archived.");

        Status = ExamStatus.Archived;
    }

    public void AssignToClass(string classId)
    {
        if (string.IsNullOrWhiteSpace(classId))
            throw new ExamDomainException("Class id is required.");

        if (!_assignedClassIds.Contains(classId))
            _assignedClassIds.Add(classId);
    }

    public void UnassignFromClass(string classId) => _assignedClassIds.Remove(classId);

    public bool IsAssignedToAnyOf(IReadOnlyCollection<string> classIds) =>
        IsPublic || _assignedClassIds.Any(classIds.Contains);

    private void EnsureEditable()
    {
        if (Status != ExamStatus.Draft)
            throw new ExamDomainException("Exam configuration can only be modified while the exam is in draft status.");
    }
}
```

- [ ] **Step 3: Thêm method repository lọc theo Level + QuestionType**

Trong `src/Services/Exam/Exam.Domain/AggregateModels/QuestionAggregate/IQuestionRepository.cs`, thêm sau dòng 20 (`GetByIdsAsync`):
```csharp
    Task<IReadOnlyCollection<Question>> GetByCategoryLevelTypeAsync(string categoryId, Level level,
        QuestionType questionType, CancellationToken cancellationToken = default);
```

Trong `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/Repositories/QuestionRepository.cs`, thêm method (theo đúng pattern `BuildFilter` server-side đã có) sau `GetByIdsAsync` (dòng 57):
```csharp
    public async Task<IReadOnlyCollection<Question>> GetByCategoryLevelTypeAsync(string categoryId, Level level,
        QuestionType questionType, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting Questions by CategoryId {CategoryId}, Level {Level}, Type {Type}.", categoryId, level, questionType);
        var filter = Builders<Question>.Filter.Eq(x => x.CategoryId, categoryId)
                     & Builders<Question>.Filter.Eq(x => x.Level, level)
                     & Builders<Question>.Filter.Eq(x => x.QuestionType, questionType);
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }
```

- [ ] **Step 4: Thay toàn bộ `ExamQuestionPoolService.cs`**

Thay toàn bộ `src/Services/Exam/Exam.Domain/Services/ExamQuestionPoolService.cs` bằng:
```csharp
using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Domain.Services;

public class ExamQuestionPoolService
{
    private readonly IQuestionRepository _questionRepository;

    public ExamQuestionPoolService(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<IReadOnlyCollection<string>> DrawQuestionIdsAsync(ExamEntity exam, CancellationToken cancellationToken = default)
    {
        var drawn = new List<string>();

        foreach (var cell in exam.Composition)
        {
            if (cell.Count == 0)
                continue;

            var candidates = await _questionRepository.GetByCategoryLevelTypeAsync(
                exam.CategoryId, cell.Level, cell.QuestionType, cancellationToken);

            if (candidates.Count < cell.Count)
                throw new ExamDomainException(
                    $"Không đủ câu hỏi mức '{LevelLabel(cell.Level)}' loại '{QuestionTypeLabel(cell.QuestionType)}' " +
                    $"trong ngân hàng để rút {cell.Count} câu (chỉ có {candidates.Count}).");

            var pool = candidates.Select(q => q.Id).ToArray();
            Random.Shared.Shuffle(pool);
            drawn.AddRange(pool.Take(cell.Count));
        }

        return drawn;
    }

    private static string LevelLabel(Level level) => level switch
    {
        Level.Easy => "Dễ",
        Level.Medium => "Trung bình",
        Level.Difficult => "Khó",
        _ => level.ToString()
    };

    private static string QuestionTypeLabel(QuestionType questionType) => questionType switch
    {
        QuestionType.SingleSelection => "Một đáp án",
        QuestionType.MultipleSelection => "Nhiều đáp án",
        _ => questionType.ToString()
    };
}
```

- [ ] **Step 5: `StartExamCommandHandler` luôn rút theo ma trận**

Trong `src/Services/Exam/Exam.Application/ExamResultAggregate/Commands/StartExam/StartExamCommandHandler.cs`, khối dòng 69-71:
```csharp
        var questionIds = exam.QuestionSelectionMode == QuestionSelectionMode.Pool
            ? await _examQuestionPoolService.DrawQuestionIdsAsync(exam, cancellationToken)
            : exam.QuestionIds;
```
Thay bằng:
```csharp
        var questionIds = await _examQuestionPoolService.DrawQuestionIdsAsync(exam, cancellationToken);
```

- [ ] **Step 6: Contracts — `ExamCompositionCellDto`, `ExamDto`, `ExamRequest`**

Thay toàn bộ `src/Services/Exam/Exam.Contracts/ExamDto.cs` bằng:
```csharp
namespace Exam.Contracts;

public record ExamCompositionCellDto(Level Level, QuestionType QuestionType, int Count);

public record ExamDto(
    string Id,
    string Name,
    string ShortDesc,
    string Content,
    TimeSpan Duration,
    Level Level,
    DateTime DateCreated,
    string OwnerUserId,
    decimal MinimumPassingScore,
    bool IsTimeRestricted,
    string CategoryId,
    string CategoryName,
    ExamStatus Status,
    IReadOnlyCollection<ExamCompositionCellDto> Composition,
    DateTime? AvailableFrom,
    DateTime? AvailableTo,
    int NumberOfQuestions,
    IReadOnlyCollection<string> AssignedClassIds,
    bool IsPublic,
    int? MaxAttempts);
```

Thay toàn bộ `src/Services/Exam/Exam.Contracts/ExamRequest.cs` bằng:
```csharp
namespace Exam.Contracts;

public record ExamRequest(
    string Name,
    string ShortDesc,
    string Content,
    TimeSpan Duration,
    Level Level,
    string CategoryId,
    bool IsTimeRestricted,
    decimal MinimumPassingScore);

public record ConfigureExamCompositionRequest(IReadOnlyCollection<ExamCompositionCellDto> Cells);

public record ScheduleExamAvailabilityRequest(DateTime? AvailableFrom, DateTime? AvailableTo);

public record ConfigureMaxAttemptsRequest(int? MaxAttempts);
```

- [ ] **Step 7: Thay toàn bộ `ExamMapper.cs`**

Thay toàn bộ `src/Services/Exam/Exam.Application/ExamAggregate/ExamMapper.cs` bằng:
```csharp
namespace Exam.Application.ExamAggregate;

internal static class ExamMapper
{
    public static ExamDto ToDto(Domain.AggregateModels.ExamAggregate.Exam exam) =>
        new(
            exam.Id,
            exam.Name,
            exam.ShortDesc,
            exam.Content,
            exam.Duration,
            exam.Level,
            exam.DateCreated,
            exam.OwnerUserId,
            exam.MinimumPassingScore,
            exam.IsTimeRestricted,
            exam.CategoryId,
            exam.CategoryName,
            exam.Status,
            exam.Composition.Select(c => new ExamCompositionCellDto(c.Level, c.QuestionType, c.Count)).ToList(),
            exam.AvailableFrom,
            exam.AvailableTo,
            exam.NumberOfQuestions,
            exam.AssignedClassIds,
            exam.IsPublic,
            exam.MaxAttempts);
}
```

- [ ] **Step 8: Tạo command `ConfigureExamComposition`**

Tạo `.../Commands/ConfigureExamComposition/ConfigureExamCompositionCommand.cs`:
```csharp
using Exam.Contracts;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ConfigureExamComposition;

public record ConfigureExamCompositionCommand(string ExamId, IReadOnlyCollection<ExamCompositionCellDto> Cells, Actor Actor) : IRequest<ExamDto>;
```

Tạo `.../Commands/ConfigureExamComposition/ConfigureExamCompositionCommandHandler.cs`:
```csharp
using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ConfigureExamComposition;

public class ConfigureExamCompositionCommandHandler : IRequestHandler<ConfigureExamCompositionCommand, ExamDto>
{
    private readonly IExamRepository _examRepository;

    public ConfigureExamCompositionCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<ExamDto> Handle(ConfigureExamCompositionCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, exam.OwnerUserId, nameof(Domain.AggregateModels.ExamAggregate.Exam), exam.Id);

        // Không kiểm tra số câu thực có trong ngân hàng ở đây - lỗi thiếu câu theo từng ô được nêu rõ khi
        // học viên bắt đầu làm bài (ExamQuestionPoolService.DrawQuestionIdsAsync), theo đúng spec.
        var cells = request.Cells
            .Select(c => new ExamCompositionCell(c.Level, c.QuestionType, c.Count))
            .ToList();

        exam.ConfigureComposition(cells);

        await _examRepository.UpdateAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
```

Tạo `.../Commands/ConfigureExamComposition/ConfigureExamCompositionCommandValidator.cs`:
```csharp
using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.ConfigureExamComposition;

public class ConfigureExamCompositionCommandValidator : AbstractValidator<ConfigureExamCompositionCommand>
{
    public ConfigureExamCompositionCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.Cells).NotNull();
        RuleForEach(x => x.Cells).Must(c => c.Count >= 0).WithMessage("Số câu mỗi ô không được âm.");
        RuleFor(x => x.Cells)
            .Must(cells => cells != null && cells.Sum(c => c.Count) > 0)
            .WithMessage("Tổng số câu trong ma trận phải lớn hơn 0.");
    }
}
```

- [ ] **Step 9: Xoá 4 thư mục command cũ**

Xoá toàn bộ các thư mục:
- `src/Services/Exam/Exam.Application/ExamAggregate/Commands/ConfigureQuestionPool/`
- `src/Services/Exam/Exam.Application/ExamAggregate/Commands/AddQuestionToExam/`
- `src/Services/Exam/Exam.Application/ExamAggregate/Commands/RemoveQuestionFromExam/`
- `src/Services/Exam/Exam.Application/ExamAggregate/Commands/ConfigureNegativeMarking/`

(Dùng `git rm -r` ở Step commit, hoặc xoá file bằng công cụ hệ thống.)

- [ ] **Step 10: `ExamsController` — bỏ 3 endpoint, đổi pool→composition**

Trong `src/Services/Exam/Exam.API/Controllers/ExamsController.cs`:

Khối `using` đầu file: xoá 4 dòng import cũ, thêm import composition. Dòng 1-6:
```csharp
using Exam.Application.ExamAggregate.Commands.AddQuestionToExam;
using Exam.Application.ExamAggregate.Commands.ArchiveExam;
using Exam.Application.ExamAggregate.Commands.AssignExamToClass;
using Exam.Application.ExamAggregate.Commands.ConfigureMaxAttempts;
using Exam.Application.ExamAggregate.Commands.ConfigureNegativeMarking;
using Exam.Application.ExamAggregate.Commands.ConfigureQuestionPool;
```
Thành:
```csharp
using Exam.Application.ExamAggregate.Commands.ArchiveExam;
using Exam.Application.ExamAggregate.Commands.AssignExamToClass;
using Exam.Application.ExamAggregate.Commands.ConfigureExamComposition;
using Exam.Application.ExamAggregate.Commands.ConfigureMaxAttempts;
```
Và xoá dòng 10 `using Exam.Application.ExamAggregate.Commands.RemoveQuestionFromExam;`.

Xoá 2 endpoint `AddQuestion` và `RemoveQuestion` (dòng 91-105).

Thay endpoint `ConfigureQuestionPool` (dòng 107-114):
```csharp
    [HttpPut("{id}/question-pool")]
    [Authorize(Policy = Permissions.Exam.ManagePool)]
    public async Task<IActionResult> ConfigureQuestionPool(string id, [FromBody] ConfigureQuestionPoolRequest request, CancellationToken cancellationToken)
    {
        var command = new ConfigureQuestionPoolCommand(id, request.PoolCategoryId, request.PoolQuestionCount, User.GetActor());
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
```
Bằng:
```csharp
    [HttpPut("{id}/composition")]
    [Authorize(Policy = Permissions.Exam.ManagePool)]
    public async Task<IActionResult> ConfigureComposition(string id, [FromBody] ConfigureExamCompositionRequest request, CancellationToken cancellationToken)
    {
        var command = new ConfigureExamCompositionCommand(id, request.Cells, User.GetActor());
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
```

Xoá endpoint `ConfigureNegativeMarking` (dòng 125-132).

- [ ] **Step 11: `MongoClassMaps` — bỏ NegativeMarkingRatio khỏi Exam, đăng ký `ExamCompositionCell`**

Trong `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/MongoClassMaps.cs`, khối `ExamEntity` (đã sửa ở Task 1) — xoá dòng `cm.MapMember(e => e.NegativeMarkingRatio)...`:
```csharp
            BsonClassMap.RegisterClassMap<ExamEntity>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(e => new ExamEntity(e.Name, e.ShortDesc, e.Content, e.Duration, e.Level,
                    e.OwnerUserId, e.CategoryId, e.CategoryName, e.IsTimeRestricted, e.MinimumPassingScore));
                cm.MapMember(e => e.MinimumPassingScore).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            });
```
Thêm ngay trước khối `ExamEntity` một khối đăng ký `ExamCompositionCell` (thêm `using Exam.Domain.AggregateModels.ExamAggregate;` nếu chưa có — hiện file dùng alias `ExamEntity`; namespace này cũng chứa `ExamCompositionCell`, thêm using đầy đủ):
```csharp
        if (!BsonClassMap.IsClassMapRegistered(typeof(Domain.AggregateModels.ExamAggregate.ExamCompositionCell)))
        {
            BsonClassMap.RegisterClassMap<Domain.AggregateModels.ExamAggregate.ExamCompositionCell>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(c => new Domain.AggregateModels.ExamAggregate.ExamCompositionCell(c.Level, c.QuestionType, c.Count));
            });
        }
```

- [ ] **Step 12: `Permissions` — bỏ 2 hằng số**

Trong `src/Services/Exam/Exam.Contracts/Permissions.cs`, class `Exam` (dòng 20-36): xoá dòng 25 `ManageQuestions` và dòng 28 `ManageNegativeMarking`.

Trong mảng `All` (dòng 59-69), thay 2 dòng 63-64:
```csharp
        Exam.Create, Exam.Update, Exam.Delete, Exam.ManageQuestions, Exam.ManagePool,
        Exam.ManageAvailability, Exam.ManageNegativeMarking, Exam.ManageMaxAttempts, Exam.Publish, Exam.Unpublish, Exam.Archive,
```
Bằng:
```csharp
        Exam.Create, Exam.Update, Exam.Delete, Exam.ManagePool,
        Exam.ManageAvailability, Exam.ManageMaxAttempts, Exam.Publish, Exam.Unpublish, Exam.Archive,
```
(`RolePermissionSeeder` không cần sửa code: cơ chế diff của nó chỉ *thêm* quyền còn thiếu vào Instructor; các quyền không còn trong `Permissions.All` sẽ đơn giản không được seed lại — việc dọn quyền rác đã gán trong DB nằm ở task dọn dữ liệu.)

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/Permissions.razor.cs`, xoá dòng 32 (`Exam.ManageQuestions`) và dòng 35 (`Exam.ManageNegativeMarking`) khỏi dictionary nhãn:
```csharp
        [Exam.Contracts.Permissions.Exam.ManageQuestions] = ("Quản lý câu hỏi trong đề", "Đề thi"),
```
và
```csharp
        [Exam.Contracts.Permissions.Exam.ManageNegativeMarking] = ("Cấu hình trừ điểm", "Đề thi"),
```

- [ ] **Step 13: Xoá enum `QuestionSelectionMode`**

Xoá file `src/Services/Exam/Exam.Contracts/Enums/QuestionSelectionMode.cs`.

- [ ] **Step 14: DataSeeder — dùng `ConfigureComposition`**

Trong `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/DataSeeder.cs`, ngay trước vòng `foreach (var seedCategory in GetSeedCategories())` (dòng 64), thêm định nghĩa ma trận dùng chung (mỗi môn seed đều có đúng 1 câu Dễ/Một đáp án, 1 Trung bình/Một đáp án, 1 Trung bình/Nhiều đáp án, 1 Khó/Một đáp án):
```csharp
        var publishedComposition = new List<ExamCompositionCell>
        {
            new(Level.Easy, QuestionType.SingleSelection, 1),
            new(Level.Medium, QuestionType.SingleSelection, 1),
            new(Level.Medium, QuestionType.MultipleSelection, 1),
            new(Level.Difficult, QuestionType.SingleSelection, 1),
        };
        var draftComposition = new List<ExamCompositionCell>
        {
            new(Level.Easy, QuestionType.SingleSelection, 1),
            new(Level.Medium, QuestionType.SingleSelection, 1),
        };
```

Thay khối tạo `publishedExam` (dòng 92-101):
```csharp
            var publishedExam = new ExamEntity(
                $"Bài kiểm tra {seedCategory.Name}", $"Đề kiểm tra tổng hợp - {seedCategory.Name}",
                $"Bài thi gồm {questionIds.Count} câu hỏi thuộc danh mục {seedCategory.Name}.",
                TimeSpan.FromMinutes(45), Level.Medium, instructor.ExternalId, category.Id, category.Name,
                isTimeRestricted: true, minimumPassingScore: 6.0m);
            foreach (var questionId in questionIds)
                publishedExam.AddQuestion(questionId);
            publishedExam.ConfigureNegativeMarking(0.25m);
            publishedExam.ScheduleAvailability(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));
            publishedExam.Publish();
```
Bằng:
```csharp
            var publishedExam = new ExamEntity(
                $"Bài kiểm tra {seedCategory.Name}", $"Đề kiểm tra tổng hợp - {seedCategory.Name}",
                $"Bài thi gồm {publishedComposition.Sum(c => c.Count)} câu hỏi thuộc danh mục {seedCategory.Name}.",
                TimeSpan.FromMinutes(45), Level.Medium, instructor.ExternalId, category.Id, category.Name,
                isTimeRestricted: true, minimumPassingScore: 6.0m);
            publishedExam.ConfigureComposition(publishedComposition);
            publishedExam.ScheduleAvailability(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));
            publishedExam.Publish();
```

Thay khối tạo `draftExam` (dòng 113-119):
```csharp
            var draftExam = new ExamEntity(
                $"Đề nháp {seedCategory.Name}", $"Đề đang soạn - {seedCategory.Name}",
                $"Bản nháp đề thi thuộc danh mục {seedCategory.Name}, chưa xuất bản.",
                TimeSpan.FromMinutes(30), Level.Easy, instructor.ExternalId, category.Id, category.Name,
                isTimeRestricted: false, minimumPassingScore: 6.0m);
            foreach (var questionId in questionIds.Take(2))
                draftExam.AddQuestion(questionId);
            await examRepository.InsertAsync(draftExam, cancellationToken);
```
Bằng:
```csharp
            var draftExam = new ExamEntity(
                $"Đề nháp {seedCategory.Name}", $"Đề đang soạn - {seedCategory.Name}",
                $"Bản nháp đề thi thuộc danh mục {seedCategory.Name}, chưa xuất bản.",
                TimeSpan.FromMinutes(30), Level.Easy, instructor.ExternalId, category.Id, category.Name,
                isTimeRestricted: false, minimumPassingScore: 6.0m);
            draftExam.ConfigureComposition(draftComposition);
            await examRepository.InsertAsync(draftExam, cancellationToken);
```
(`using Exam.Domain.AggregateModels.ExamAggregate;` đã có ở dòng 5; `ExamCompositionCell` cùng namespace nên không cần thêm using.)

- [ ] **Step 15: WebApp — routes, client, ExamFormResult, ExamFormDialog, Exams**

`src/Web/Exam.WebApp/Services/ApiRoutes.cs`, class `Exams` (dòng 53-69): xoá `Question(...)`, `NegativeMarking(...)`, đổi `QuestionPool`→`Composition`. Thay dòng 58, 59, 61:
```csharp
        public static string Question(string examId, string questionId) => $"{Base}/{examId}/questions/{questionId}";
        public static string QuestionPool(string examId) => $"{Base}/{examId}/question-pool";
        public static string Availability(string examId) => $"{Base}/{examId}/availability";
        public static string NegativeMarking(string examId) => $"{Base}/{examId}/negative-marking";
```
Bằng:
```csharp
        public static string Composition(string examId) => $"{Base}/{examId}/composition";
        public static string Availability(string examId) => $"{Base}/{examId}/availability";
```

`src/Web/Exam.WebApp/Services/ExamApiClient.cs`: xoá `AddQuestionToExamAsync` (dòng 92-93), `RemoveQuestionFromExamAsync` (dòng 95-96), `ConfigureNegativeMarkingAsync` (dòng 104-105); đổi `ConfigureQuestionPoolAsync` (dòng 98-99):
```csharp
    public Task<ExamDto> ConfigureQuestionPoolAsync(string examId, ConfigureQuestionPoolRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Put, ApiRoutes.Exams.QuestionPool(examId), body, cancellationToken);
```
Bằng:
```csharp
    public Task<ExamDto> ConfigureExamCompositionAsync(string examId, ConfigureExamCompositionRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Put, ApiRoutes.Exams.Composition(examId), body, cancellationToken);
```

Thay toàn bộ `src/Web/Exam.WebApp/Components/Pages/Admin/ExamFormResult.cs` bằng:
```csharp
namespace Exam.WebApp.Components.Pages.Admin;

// Gộp kết quả form tạo/sửa đề thi: thông tin cơ bản + các cấu hình tuỳ chọn áp dụng tuần tự sau khi
// tạo/sửa. Field nào null nghĩa là không thay đổi/không bật.
public record ExamFormResult(
    ExamRequest Exam,
    ScheduleExamAvailabilityRequest? Availability,
    ConfigureExamCompositionRequest? Composition,
    ConfigureMaxAttemptsRequest? MaxAttempts);
```

Thay toàn bộ `src/Web/Exam.WebApp/Components/Pages/Admin/ExamFormDialog.razor` bằng:
```razor
@inherits FormDialogBase

<AppForm @ref="form">
    <AppTextField Label="Tên đề thi" @bind-Value="name" Required="true" RequiredError="Bắt buộc nhập tên." />
    <AppTextField Label="Mô tả ngắn" @bind-Value="shortDesc" />
    <AppTextField Label="Nội dung / hướng dẫn" @bind-Value="content" Lines="2" />

    <div class="field-row">
        <AppNumericField TValue="int" Label="Thời lượng (phút)" @bind-Value="durationMinutes" Min="1" Required="true" />
        <AppSelect TValue="Level" Label="Mức độ" @bind-Value="level">
            <option value="@Level.Easy">Dễ</option>
            <option value="@Level.Medium">Trung bình</option>
            <option value="@Level.Difficult">Khó</option>
        </AppSelect>
    </div>

    <div class="field-row">
        <AppNumericField TValue="decimal" Label="Điểm đạt tối thiểu (0–10)" @bind-Value="minimumPassingScore" Min="0" Max="10" Step="0.5" Required="true" />
        <div style="margin-top:22px;">
            <AppSwitch Value="isTimeRestricted" ValueChanged="v => isTimeRestricted = v">Giới hạn thời gian làm bài</AppSwitch>
        </div>
    </div>
</AppForm>

<div style="font-size:13px;font-weight:700;margin:14px 0 6px;">Ma trận cấu trúc đề thi</div>
<p class="page-sub">Nhập số câu cần rút ngẫu nhiên cho từng mức độ × loại câu hỏi (trong đúng môn học của đề).</p>
<table class="data">
    <thead>
        <tr><th>Mức độ</th><th>Một đáp án</th><th>Nhiều đáp án</th></tr>
    </thead>
    <tbody>
        <tr>
            <td>Dễ</td>
            <td><AppNumericField TValue="int" @bind-Value="easySingle" Min="0" Disabled="isLocked" /></td>
            <td><AppNumericField TValue="int" @bind-Value="easyMulti" Min="0" Disabled="isLocked" /></td>
        </tr>
        <tr>
            <td>Trung bình</td>
            <td><AppNumericField TValue="int" @bind-Value="mediumSingle" Min="0" Disabled="isLocked" /></td>
            <td><AppNumericField TValue="int" @bind-Value="mediumMulti" Min="0" Disabled="isLocked" /></td>
        </tr>
        <tr>
            <td>Khó</td>
            <td><AppNumericField TValue="int" @bind-Value="difficultSingle" Min="0" Disabled="isLocked" /></td>
            <td><AppNumericField TValue="int" @bind-Value="difficultMulti" Min="0" Disabled="isLocked" /></td>
        </tr>
    </tbody>
</table>
<p class="page-sub">Tổng số câu: <b>@CompositionTotal</b></p>
@if (compositionError != null)
{
    <AppAlert Severity="AppAlertSeverity.Warning">@compositionError</AppAlert>
}

<details class="advanced-panel">
    <summary>Cấu hình nâng cao (tuỳ chọn)</summary>
    <div class="body">
        <AppSwitch Value="enableAvailability" ValueChanged="v => enableAvailability = v" Disabled="isArchived">Giới hạn thời gian phát hành</AppSwitch>
        @if (enableAvailability)
        {
            <div class="field-row">
                <AppDateField Label="Mở từ ngày" @bind-Value="availableFrom" Disabled="isArchived" />
                <AppDateField Label="Đóng vào ngày" @bind-Value="availableTo" Disabled="isArchived" />
            </div>
        }

        <AppSwitch Value="enableMaxAttempts" ValueChanged="v => enableMaxAttempts = v" Disabled="isLocked">Giới hạn số lần thi lại</AppSwitch>
        @if (enableMaxAttempts)
        {
            <div style="max-width:200px;">
                <AppNumericField TValue="int" Label="Số lần thi tối đa" @bind-Value="maxAttempts" Min="1" Disabled="isLocked" />
            </div>
        }
    </div>
</details>

<div class="modal-actions">
    <AppButton OnClick="Cancel">Huỷ</AppButton>
    <AppButton Variant="AppButtonVariant.Primary" OnClick="Submit">Lưu</AppButton>
</div>
```

Thay toàn bộ `src/Web/Exam.WebApp/Components/Pages/Admin/ExamFormDialog.razor.cs` bằng:
```csharp
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ExamFormDialog : FormDialogBase
{
    [Parameter]
    public string CategoryId { get; set; } = "";

    [Parameter]
    public ExamDto? Model { get; set; }

    private string name = "";
    private string shortDesc = "";
    private string content = "";
    private int durationMinutes = 30;
    private Level level = Level.Easy;
    private decimal minimumPassingScore = 5m;
    private bool isTimeRestricted = true;

    private int easySingle;
    private int easyMulti;
    private int mediumSingle;
    private int mediumMulti;
    private int difficultSingle;
    private int difficultMulti;
    private string? compositionError;

    private bool enableAvailability;
    private DateTime? availableFrom;
    private DateTime? availableTo;

    private bool enableMaxAttempts;
    private int maxAttempts = 1;

    // Chỉ khoá cấu hình khi sửa đề thi đã rời trạng thái Nháp - đề mới tạo luôn Draft.
    private bool isLocked => Model != null && Model.Status != ExamStatus.Draft;
    private bool isArchived => Model != null && Model.Status == ExamStatus.Archived;

    private int CompositionTotal => easySingle + easyMulti + mediumSingle + mediumMulti + difficultSingle + difficultMulti;

    protected override void OnInitialized()
    {
        if (Model != null)
        {
            name = Model.Name;
            shortDesc = Model.ShortDesc;
            content = Model.Content;
            durationMinutes = (int)Model.Duration.TotalMinutes;
            level = Model.Level;
            minimumPassingScore = Model.MinimumPassingScore;
            isTimeRestricted = Model.IsTimeRestricted;

            easySingle = CountOf(Level.Easy, QuestionType.SingleSelection);
            easyMulti = CountOf(Level.Easy, QuestionType.MultipleSelection);
            mediumSingle = CountOf(Level.Medium, QuestionType.SingleSelection);
            mediumMulti = CountOf(Level.Medium, QuestionType.MultipleSelection);
            difficultSingle = CountOf(Level.Difficult, QuestionType.SingleSelection);
            difficultMulti = CountOf(Level.Difficult, QuestionType.MultipleSelection);

            enableAvailability = Model.AvailableFrom.HasValue || Model.AvailableTo.HasValue;
            availableFrom = Model.AvailableFrom;
            availableTo = Model.AvailableTo;

            enableMaxAttempts = Model.MaxAttempts.HasValue;
            if (Model.MaxAttempts.HasValue)
                maxAttempts = Model.MaxAttempts.Value;
        }
    }

    private int CountOf(Level level, QuestionType questionType) =>
        Model!.Composition.FirstOrDefault(c => c.Level == level && c.QuestionType == questionType)?.Count ?? 0;

    private ConfigureExamCompositionRequest BuildCompositionRequest()
    {
        var cells = new List<ExamCompositionCellDto>();
        void Add(Level l, QuestionType t, int n)
        {
            if (n > 0)
                cells.Add(new ExamCompositionCellDto(l, t, n));
        }
        Add(Level.Easy, QuestionType.SingleSelection, easySingle);
        Add(Level.Easy, QuestionType.MultipleSelection, easyMulti);
        Add(Level.Medium, QuestionType.SingleSelection, mediumSingle);
        Add(Level.Medium, QuestionType.MultipleSelection, mediumMulti);
        Add(Level.Difficult, QuestionType.SingleSelection, difficultSingle);
        Add(Level.Difficult, QuestionType.MultipleSelection, difficultMulti);
        return new ConfigureExamCompositionRequest(cells);
    }

    private async Task Submit()
    {
        compositionError = null;

        if (!await ValidateAsync())
            return;

        // Ma trận rỗng chỉ chấp nhận khi ĐANG sửa đề đã khoá (không đụng tới cấu hình cũ). Với đề mới hoặc
        // đề Draft đang sửa, phải có ít nhất 1 câu - nếu không, đề không thể publish (NumberOfQuestions == 0).
        if (!isLocked && CompositionTotal == 0)
        {
            compositionError = "Ma trận phải có tổng số câu lớn hơn 0.";
            return;
        }

        var request = new ExamRequest(
            name,
            shortDesc,
            content,
            TimeSpan.FromMinutes(durationMinutes),
            level,
            CategoryId,
            isTimeRestricted,
            minimumPassingScore);

        var availability = enableAvailability ? new ScheduleExamAvailabilityRequest(availableFrom, availableTo) : null;
        var composition = !isLocked && CompositionTotal > 0 ? BuildCompositionRequest() : null;
        var maxAttemptsRequest = enableMaxAttempts ? new ConfigureMaxAttemptsRequest(maxAttempts) : null;

        Dialog.Close(AppDialogResult.Ok(new ExamFormResult(request, availability, composition, maxAttemptsRequest)));
    }
}
```

Thay toàn bộ `src/Web/Exam.WebApp/Components/Pages/Admin/Exams.razor.cs` bằng:
```csharp
using Exam.WebApp.Components.UI;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Exams : AdminPageBase
{
    [Parameter] public string? Id { get; set; }

    private IReadOnlyCollection<CategoryDto>? categories;
    private IReadOnlyCollection<ExamDto>? exams;
    private string? selectedCategoryId;
    private ExamDto? selected;
    private UserDto? currentUser;

    // Khớp đúng OwnershipGuard.EnsureOwnerOrAdmin ở backend (Admin luôn được, Instructor chỉ được với đề
    // thi do chính mình tạo) - tính trước ở UI để ẩn/khoá các hành động chắc chắn sẽ bị 403 thay vì để
    // người dùng bấm rồi mới thấy toast lỗi.
    private bool CanManageSelected => currentUser != null && selected != null
        && (currentUser.Role == UserRole.Admin || currentUser.ExternalId == selected.OwnerUserId);

    private IReadOnlyCollection<ClassRoomDto> allClasses = [];
    private DateTime? availableFrom;
    private DateTime? availableTo;
    private bool limitMaxAttempts;
    private int maxAttempts = 1;
    private string? selectedClassId;

    private int easySingle;
    private int easyMulti;
    private int mediumSingle;
    private int mediumMulti;
    private int difficultSingle;
    private int difficultMulti;

    private int CompositionTotal => easySingle + easyMulti + mediumSingle + mediumMulti + difficultSingle + difficultMulti;

    private IReadOnlyCollection<ExamResultAdminListItemDto>? results;
    private string resultsFilter = "all";
    private ExamResultAdminListItemDto? drawerResult;
    private ExamAttemptStatusDto? drawerStatus;

    private IReadOnlyCollection<ClassRoomDto> unassignedClasses =>
        allClasses.Where(c => selected != null && !selected.AssignedClassIds.Contains(c.Id)).ToList();

    protected override async Task OnInitializedAsync() =>
        await ExecuteAsync(async () =>
        {
            currentUser = await Api.GetMeAsync();
            categories = await Api.GetCategoriesAsync();
        }, "Không tải được danh sách môn học");

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(Id))
        {
            selected = null;
            return;
        }

        if (selected?.Id == Id)
            return;

        await LoadSelectedAsync(Id);
    }

    private Task LoadSelectedAsync(string id) => ExecuteAsync(async () =>
    {
        selected = await Api.GetExamByIdAsync(id);
        selectedCategoryId ??= selected.CategoryId;
        availableFrom = selected.AvailableFrom;
        availableTo = selected.AvailableTo;
        limitMaxAttempts = selected.MaxAttempts.HasValue;
        maxAttempts = selected.MaxAttempts ?? 1;
        allClasses = await Api.GetClassesAsync();
        selectedClassId = null;

        easySingle = CompositionCountOf(Level.Easy, QuestionType.SingleSelection);
        easyMulti = CompositionCountOf(Level.Easy, QuestionType.MultipleSelection);
        mediumSingle = CompositionCountOf(Level.Medium, QuestionType.SingleSelection);
        mediumMulti = CompositionCountOf(Level.Medium, QuestionType.MultipleSelection);
        difficultSingle = CompositionCountOf(Level.Difficult, QuestionType.SingleSelection);
        difficultMulti = CompositionCountOf(Level.Difficult, QuestionType.MultipleSelection);

        resultsFilter = "all";
        drawerResult = null;
        drawerStatus = null;
        results = null;
        // Kết quả thi cũng bị OwnershipGuard chặn ở backend giống Sửa/Xuất bản/Lưu trữ - không gọi API
        // này khi chắc chắn sẽ bị 403 (Instructor xem đề thi của người khác), tránh toast lỗi vô nghĩa.
        if (CanManageSelected)
            await LoadResultsAsync();
    }, "Không tải được đề thi");

    private int CompositionCountOf(Level level, QuestionType questionType) =>
        selected!.Composition.FirstOrDefault(c => c.Level == level && c.QuestionType == questionType)?.Count ?? 0;

    private Task LoadResultsAsync() => ExecuteAsync(async () =>
    {
        var result = await Api.GetExamResultsByExamAsync(selected!.Id, 1, 100);
        results = result.Items;
    }, "Không tải được kết quả thi");

    private void SetResultsFilter(string filter) => resultsFilter = filter;

    private IEnumerable<ExamResultAdminListItemDto> FilteredResults() => resultsFilter switch
    {
        "doing" => results!.Where(r => !r.Finished),
        "done" => results!.Where(r => r.Finished),
        _ => results!
    };

    private async Task OpenDrawerAsync(ExamResultAdminListItemDto result)
    {
        drawerResult = result;
        drawerStatus = null;
        await ExecuteAsync(async () => drawerStatus = await Api.GetExamAttemptAdminStatusAsync(result.Id), "Không tải được tiến độ bài thi");
    }

    private bool IsAnswered(string questionId) =>
        drawerStatus?.Attempt?.SelectedAnswers.Any(a => a.QuestionId == questionId && a.SelectedAnswerIds.Count > 0) == true;

    private void CloseDrawer()
    {
        drawerResult = null;
        drawerStatus = null;
    }

    private Task ForceSubmitAsync() => ConfirmAndExecuteAsync(
        "Xác nhận buộc nộp bài", $"Buộc nộp bài của \"{drawerResult!.FullName}\"? Học viên sẽ không thể trả lời thêm.",
        async () =>
        {
            await Api.AdminForceFinishExamAsync(drawerResult.Id);
            CloseDrawer();
            await LoadResultsAsync();
        }, "Buộc nộp bài thất bại", $"Đã buộc nộp bài của {drawerResult!.FullName}", yesText: "Buộc nộp");

    private async Task OnCategoryChangedAsync(string categoryId)
    {
        selectedCategoryId = categoryId;
        await LoadListAsync();
    }

    private Task LoadListAsync()
    {
        if (string.IsNullOrEmpty(selectedCategoryId))
        {
            exams = [];
            return Task.CompletedTask;
        }

        return ExecuteAsync(async () =>
        {
            var result = await Api.GetExamsByCategoryAsync(selectedCategoryId, 1, 100);
            exams = result.Items;
        }, "Không tải được danh sách đề thi");
    }

    private void SelectExam(string id) => Navigation.NavigateTo($"/admin/exams/{id}");

    private async Task OpenCreateDialog()
    {
        var parameters = new Dictionary<string, object> { ["CategoryId"] = selectedCategoryId! };
        var data = await ShowFormDialogAsync<ExamFormDialog, ExamFormResult>("Thêm đề thi", parameters, new AppDialogOptions { Wide = true });
        if (data == null)
            return;

        string? createdId = null;
        await ExecuteAsync(async () =>
        {
            var created = await Api.CreateExamAsync(data.Exam);
            createdId = created.Id;
            await ApplyAdvancedConfigAsync(created.Id, data);
        }, "Tạo thất bại", "Đã thêm đề thi.");
        await LoadListAsync();
        if (createdId != null)
            SelectExam(createdId);
    }

    private async Task OpenEditDialog(ExamDto exam)
    {
        var parameters = new Dictionary<string, object>
        {
            ["CategoryId"] = exam.CategoryId,
            ["Model"] = exam
        };
        var data = await ShowFormDialogAsync<ExamFormDialog, ExamFormResult>("Sửa đề thi", parameters, new AppDialogOptions { Wide = true });
        if (data == null)
            return;

        await ExecuteAsync(async () =>
        {
            await Api.UpdateExamAsync(exam.Id, data.Exam);
            await ApplyAdvancedConfigAsync(exam.Id, data);
        }, "Cập nhật thất bại", "Đã cập nhật.");
        await LoadListAsync();
        if (selected?.Id == exam.Id)
            await LoadSelectedAsync(exam.Id);
    }

    // Availability/Composition/MaxAttempts vẫn là API riêng ở backend (policy quyền khác nhau) - gọi tuần
    // tự sau khi tạo/sửa đề thi thay vì bắt admin mở lại Chi tiết đề thi để cấu hình từng phần.
    private async Task ApplyAdvancedConfigAsync(string examId, ExamFormResult data)
    {
        if (data.Availability != null)
            await Api.ScheduleExamAvailabilityAsync(examId, data.Availability);
        if (data.Composition != null)
            await Api.ConfigureExamCompositionAsync(examId, data.Composition);
        if (data.MaxAttempts != null)
            await Api.ConfigureMaxAttemptsAsync(examId, data.MaxAttempts);
    }

    private Task DeleteAsync(ExamDto exam) => ConfirmAndExecuteAsync(
        "Xác nhận xoá", $"Xoá đề thi '{exam.Name}'?",
        async () =>
        {
            await Api.DeleteExamAsync(exam.Id);
            if (selected?.Id == exam.Id)
            {
                selected = null;
                Navigation.NavigateTo("/admin/exams");
            }
            await LoadListAsync();
        },
        "Xoá thất bại", "Đã xoá.");

    private string ClassName(string classId) => allClasses.FirstOrDefault(c => c.Id == classId)?.Name ?? "(Lớp không còn tồn tại)";

    private Task AssignClassAsync() => ExecuteAsync(async () =>
    {
        selected = await Api.AssignExamToClassAsync(selected!.Id, selectedClassId!);
        selectedClassId = null;
    }, "Gán lớp thất bại", "Đã gán lớp.");

    private Task UnassignClassAsync(string classId) => ExecuteAsync(async () =>
    {
        selected = await Api.UnassignExamFromClassAsync(selected!.Id, classId);
    }, "Bỏ gán thất bại", "Đã bỏ gán lớp.");

    private Task SaveCompositionAsync()
    {
        if (CompositionTotal == 0)
        {
            Toast.Add("Ma trận phải có tổng số câu lớn hơn 0.", AppSeverity.Error);
            return Task.CompletedTask;
        }

        return ExecuteAsync(async () =>
        {
            selected = await Api.ConfigureExamCompositionAsync(selected!.Id, BuildCompositionRequest());
        }, "Lưu thất bại", "Đã lưu ma trận cấu trúc.");
    }

    private ConfigureExamCompositionRequest BuildCompositionRequest()
    {
        var cells = new List<ExamCompositionCellDto>();
        void Add(Level l, QuestionType t, int n)
        {
            if (n > 0)
                cells.Add(new ExamCompositionCellDto(l, t, n));
        }
        Add(Level.Easy, QuestionType.SingleSelection, easySingle);
        Add(Level.Easy, QuestionType.MultipleSelection, easyMulti);
        Add(Level.Medium, QuestionType.SingleSelection, mediumSingle);
        Add(Level.Medium, QuestionType.MultipleSelection, mediumMulti);
        Add(Level.Difficult, QuestionType.SingleSelection, difficultSingle);
        Add(Level.Difficult, QuestionType.MultipleSelection, difficultMulti);
        return new ConfigureExamCompositionRequest(cells);
    }

    private Task SaveAvailabilityAsync() => ExecuteAsync(async () =>
    {
        selected = await Api.ScheduleExamAvailabilityAsync(selected!.Id, new ScheduleExamAvailabilityRequest(availableFrom, availableTo));
    }, "Lưu thất bại", "Đã lưu lịch phát hành.");

    private Task SaveMaxAttemptsAsync() => ExecuteAsync(async () =>
    {
        selected = await Api.ConfigureMaxAttemptsAsync(selected!.Id, new ConfigureMaxAttemptsRequest(limitMaxAttempts ? maxAttempts : null));
    }, "Lưu thất bại", "Đã lưu.");

    private Task TogglePublishAsync() => ExecuteAsync(async () =>
    {
        selected = selected!.Status == ExamStatus.Published
            ? await Api.UnpublishExamAsync(selected.Id)
            : await Api.PublishExamAsync(selected.Id);
    }, "Thao tác thất bại", "Đã cập nhật trạng thái.");

    private Task ArchiveAsync() => ConfirmAndExecuteAsync(
        "Xác nhận lưu trữ", $"Lưu trữ đề thi '{selected!.Name}'? Không thể hoàn tác.",
        async () => selected = await Api.ArchiveExamAsync(selected.Id),
        "Lưu trữ thất bại", "Đã lưu trữ.", yesText: "Lưu trữ");

    private static AppStatusPillVariant StatusVariant(ExamStatus status) => status.ToPillVariant();
}
```

Thay toàn bộ `src/Web/Exam.WebApp/Components/Pages/Admin/Exams.razor` bằng:
```razor
@page "/admin/exams"
@page "/admin/exams/{Id}"
@attribute [Authorize(Roles = "Instructor,Admin")]
@inherits AdminPageBase
@inject NavigationManager Navigation

<PageTitle>Quản lý đề thi</PageTitle>

<div class="page-header-row"><h1 class="page-title" style="margin:0;">Quản lý đề thi</h1></div>
<p class="page-sub">Chọn 1 đề thi bên trái để xem/chỉnh sửa chi tiết — không cần chuyển trang.</p>

<div class="master-detail">
    <aside class="md-list">
        <div class="md-list-header">
            <CategorySelector Categories="categories" Value="@selectedCategoryId" ValueChanged="OnCategoryChangedAsync" />
        </div>
        <div class="md-list-items">
            @if (categories == null)
            {
                <AppSpinner />
            }
            else if (string.IsNullOrEmpty(selectedCategoryId))
            {
                <div class="empty-state">Chọn môn học để xem danh sách đề thi.</div>
            }
            else if (exams == null)
            {
                <AppSpinner />
            }
            else if (exams.Count == 0)
            {
                <div class="empty-state">Chưa có đề thi nào trong môn học này.</div>
            }
            else
            {
                @foreach (var exam in exams)
                {
                    <div class="md-item @(exam.Id == selected?.Id ? "active" : "")" @onclick="() => SelectExam(exam.Id)">
                        <div class="t">@exam.Name</div>
                        <div class="m"><AppStatusPill Variant="StatusVariant(exam.Status)">@exam.Status.ToLabel()</AppStatusPill>@exam.NumberOfQuestions câu · @exam.Duration.TotalMinutes phút</div>
                    </div>
                }
            }
        </div>
        <div class="md-list-footer">
            <AppButton Variant="AppButtonVariant.Primary" FullWidth="true" Disabled="string.IsNullOrEmpty(selectedCategoryId)" OnClick="OpenCreateDialog">+ Thêm đề thi</AppButton>
        </div>
    </aside>
    <section class="md-detail">
        @if (selected == null)
        {
            <div class="md-empty"><div class="big">📄</div>Chọn một đề thi bên trái để xem chi tiết</div>
        }
        else
        {
            <div class="exam-header">
                <h2>@selected.Name</h2>
                <AppStatusPill Variant="StatusVariant(selected.Status)">@selected.Status.ToLabel()</AppStatusPill>
                <span class="spacer"></span>
                <AppButton OnClick="() => OpenEditDialog(selected)" Disabled="!CanManageSelected || selected.Status != ExamStatus.Draft" title="Chỉ sửa được khi đang Nháp">✎ Sửa</AppButton>
                @if (selected.Status != ExamStatus.Archived)
                {
                    <AppButton Disabled="!CanManageSelected" OnClick="TogglePublishAsync">@(selected.Status == ExamStatus.Published ? "Chuyển về Nháp" : "Xuất bản")</AppButton>
                    <AppButton Variant="AppButtonVariant.Danger" Disabled="!CanManageSelected" OnClick="ArchiveAsync">Lưu trữ</AppButton>
                }
                <AppButton Variant="AppButtonVariant.Danger" Disabled="!CanManageSelected" OnClick="() => DeleteAsync(selected)">Xoá</AppButton>
            </div>

            @if (!CanManageSelected)
            {
                <AppAlert Severity="AppAlertSeverity.Warning">Bạn không phải người tạo đề thi này nên chỉ có thể xem, không thể chỉnh sửa hoặc xem kết quả thi.</AppAlert>
            }
            else if (selected.Status != ExamStatus.Draft)
            {
                <AppAlert Severity="AppAlertSeverity.Info">Chỉ có thể sửa ma trận cấu trúc khi đề thi ở trạng thái Nháp.</AppAlert>
            }

            <div class="exam-grid2">
                <div style="display:flex;flex-direction:column;gap:14px;">
                    <AppCard>
                        <h3>Thông tin chung</h3>
                        <div class="info-row"><span>Mô tả</span><b>@selected.ShortDesc</b></div>
                        <div class="info-row"><span>Thời lượng</span><b>@selected.Duration.TotalMinutes phút</b></div>
                        <div class="info-row"><span>Mức độ</span><b>@selected.Level.ToLabel()</b></div>
                        <div class="info-row"><span>Điểm đạt</span><b>@selected.MinimumPassingScore</b></div>
                        <div class="info-row"><span>Số câu hỏi</span><b>@selected.NumberOfQuestions</b></div>
                        <div class="info-row"><span>Số lần thi tối đa</span><b>@(selected.MaxAttempts.HasValue ? $"{selected.MaxAttempts} lần" : "Không giới hạn")</b></div>
                    </AppCard>

                    <AppCard>
                        <h3>Lịch phát hành</h3>
                        <div class="field-row">
                            <AppDateField Label="Mở từ ngày" @bind-Value="availableFrom" Disabled="!CanManageSelected || selected.Status == ExamStatus.Archived" />
                            <AppDateField Label="Đóng vào ngày" @bind-Value="availableTo" Disabled="!CanManageSelected || selected.Status == ExamStatus.Archived" />
                        </div>
                        <AppButton Variant="AppButtonVariant.Primary" Disabled="!CanManageSelected || selected.Status == ExamStatus.Archived" OnClick="SaveAvailabilityAsync">Lưu lịch phát hành</AppButton>
                    </AppCard>

                    <AppCard>
                        <h3>Giới hạn số lần thi lại</h3>
                        <AppSwitch Value="limitMaxAttempts" ValueChanged="v => limitMaxAttempts = v" Disabled="!CanManageSelected || selected.Status != ExamStatus.Draft">Giới hạn số lần thi</AppSwitch>
                        @if (limitMaxAttempts)
                        {
                            <AppNumericField TValue="int" Label="Số lần thi tối đa" @bind-Value="maxAttempts" Min="1" Disabled="!CanManageSelected || selected.Status != ExamStatus.Draft" />
                        }
                        <AppButton Variant="AppButtonVariant.Primary" Disabled="!CanManageSelected || selected.Status != ExamStatus.Draft" OnClick="SaveMaxAttemptsAsync">Lưu</AppButton>
                    </AppCard>
                </div>

                <div style="display:flex;flex-direction:column;gap:14px;">
                    <AppCard>
                        <h3>Ma trận cấu trúc đề thi</h3>
                        <p class="page-sub">Số câu rút ngẫu nhiên cho từng mức độ × loại câu hỏi (trong đúng môn học của đề).</p>
                        <table class="data">
                            <thead>
                                <tr><th>Mức độ</th><th>Một đáp án</th><th>Nhiều đáp án</th></tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>Dễ</td>
                                    <td><AppNumericField TValue="int" @bind-Value="easySingle" Min="0" Disabled="!CanManageSelected || selected.Status != ExamStatus.Draft" /></td>
                                    <td><AppNumericField TValue="int" @bind-Value="easyMulti" Min="0" Disabled="!CanManageSelected || selected.Status != ExamStatus.Draft" /></td>
                                </tr>
                                <tr>
                                    <td>Trung bình</td>
                                    <td><AppNumericField TValue="int" @bind-Value="mediumSingle" Min="0" Disabled="!CanManageSelected || selected.Status != ExamStatus.Draft" /></td>
                                    <td><AppNumericField TValue="int" @bind-Value="mediumMulti" Min="0" Disabled="!CanManageSelected || selected.Status != ExamStatus.Draft" /></td>
                                </tr>
                                <tr>
                                    <td>Khó</td>
                                    <td><AppNumericField TValue="int" @bind-Value="difficultSingle" Min="0" Disabled="!CanManageSelected || selected.Status != ExamStatus.Draft" /></td>
                                    <td><AppNumericField TValue="int" @bind-Value="difficultMulti" Min="0" Disabled="!CanManageSelected || selected.Status != ExamStatus.Draft" /></td>
                                </tr>
                            </tbody>
                        </table>
                        <p class="page-sub">Tổng số câu: <b>@CompositionTotal</b></p>
                        <AppButton Variant="AppButtonVariant.Primary" Disabled="!CanManageSelected || selected.Status != ExamStatus.Draft" OnClick="SaveCompositionAsync">Lưu ma trận</AppButton>
                    </AppCard>

                    <AppCard>
                        <h3>Lớp được giao</h3>
                        @if (selected.IsPublic)
                        {
                            <AppAlert Severity="AppAlertSeverity.Info">Đề công khai - mọi học viên đăng nhập đều thi được.</AppAlert>
                        }
                        else
                        {
                            @foreach (var classId in selected.AssignedClassIds)
                            {
                                <div class="class-row">
                                    <span>@ClassName(classId)</span>
                                    <AppIconButton Danger="true" Disabled="!CanManageSelected" OnClick="() => UnassignClassAsync(classId)">✕</AppIconButton>
                                </div>
                            }
                        }

                        @if (unassignedClasses.Count > 0)
                        {
                            <hr style="border:none;border-top:1px solid var(--line);margin:14px 0;" />
                            <AppSelect TValue="string" Label="Gán thêm lớp" Disabled="!CanManageSelected" @bind-Value="selectedClassId">
                                <option value="">— Chọn lớp —</option>
                                @foreach (var c in unassignedClasses)
                                {
                                    <option value="@c.Id">@c.Name</option>
                                }
                            </AppSelect>
                            <AppButton Variant="AppButtonVariant.Primary" Disabled="!CanManageSelected || string.IsNullOrEmpty(selectedClassId)" OnClick="AssignClassAsync">Gán lớp</AppButton>
                        }
                    </AppCard>
                </div>
            </div>

            <AppCard Class="results-card">
                <div style="display:flex;align-items:center;gap:10px;margin-bottom:12px;flex-wrap:wrap;">
                    <h3 style="margin:0;">Kết quả thi</h3><span class="spacer"></span>
                    @if (CanManageSelected)
                    {
                        <AppButton OnClick="LoadResultsAsync">↻ Làm mới</AppButton>
                    }
                </div>
                @if (!CanManageSelected)
                {
                    <div class="empty-state">Chỉ người tạo đề thi hoặc Admin mới xem được kết quả thi.</div>
                }
                else if (results == null)
                {
                    <AppSpinner />
                }
                else
                {
                    <div class="tabs">
                        <button class="tab @(resultsFilter == "all" ? "active" : "")" @onclick='() => SetResultsFilter("all")'>Tất cả (@results.Count)</button>
                        <button class="tab @(resultsFilter == "doing" ? "active" : "")" @onclick='() => SetResultsFilter("doing")'><span class="live-dot"></span>Đang làm (@results.Count(r => !r.Finished))</button>
                        <button class="tab @(resultsFilter == "done" ? "active" : "")" @onclick='() => SetResultsFilter("done")'>Đã nộp (@results.Count(r => r.Finished))</button>
                    </div>
                    <table class="data">
                        <thead><tr><th>Thí sinh</th><th>Điểm</th><th>Kết quả</th><th>Bắt đầu</th><th>Nộp bài</th><th>Trạng thái</th></tr></thead>
                        <tbody>
                            @if (!FilteredResults().Any())
                            {
                                <tr><td colspan="6"><div class="empty-state">Chưa có thí sinh nào làm đề thi này.</div></td></tr>
                            }
                            else
                            {
                                @foreach (var result in FilteredResults())
                                {
                                    <tr class="row-clickable" @onclick="() => OpenDrawerAsync(result)">
                                        <td>@result.FullName (@result.Email)</td>
                                        <td>@(result.Finished ? $"{result.TotalScore}/{result.MaxPossibleScore}" : "—")</td>
                                        <td>
                                            @if (result.Passed.HasValue)
                                            {
                                                <AppStatusPill Variant="@(result.Passed.Value ? AppStatusPillVariant.Success : AppStatusPillVariant.Danger)">@(result.Passed.Value ? "Đạt" : "Không đạt")</AppStatusPill>
                                            }
                                            else
                                            {
                                                <span>—</span>
                                            }
                                        </td>
                                        <td>@result.ExamStartDate.ToLocalTime().ToString("HH:mm dd/MM")</td>
                                        <td>@(result.ExamFinishDate.HasValue ? result.ExamFinishDate.Value.ToLocalTime().ToString("HH:mm dd/MM") : "—")</td>
                                        <td>
                                            @if (result.Finished)
                                            {
                                                <AppStatusPill Variant="AppStatusPillVariant.Success">Đã nộp</AppStatusPill>
                                            }
                                            else
                                            {
                                                <AppStatusPill Variant="AppStatusPillVariant.Info"><span class="live-dot"></span>Đang làm</AppStatusPill>
                                            }
                                        </td>
                                    </tr>
                                }
                            }
                        </tbody>
                    </table>
                }
            </AppCard>
        }
    </section>
</div>

@if (drawerResult != null)
{
    <div class="drawer-overlay open" @onclick="CloseDrawer">
        <div class="drawer" @onclick:stopPropagation="true">
            <button class="close" @onclick="CloseDrawer">✕</button>
            <h3>@drawerResult.FullName</h3>
            <p class="sub">@drawerResult.Email</p>
            @if (drawerStatus == null)
            {
                <AppSpinner />
            }
            else
            {
                <div class="drawer-meta">
                    @if (drawerStatus.Finished)
                    {
                        <div class="row"><span>Điểm</span><b>@drawerStatus.Result!.TotalScore/@drawerStatus.Result.MaxPossibleScore</b></div>
                        <div class="row"><span>Kết quả</span><b>@(drawerStatus.Result.Passed == true ? "Đạt" : "Không đạt")</b></div>
                        <div class="row"><span>Trạng thái</span><b>Đã nộp bài</b></div>
                    }
                    else
                    {
                        <div class="row"><span>Đã trả lời</span><b>@drawerStatus.Attempt!.SelectedAnswers.Count(a => a.SelectedAnswerIds.Count > 0)/@drawerStatus.Attempt.Questions.Count</b></div>
                        <div class="row"><span>Trạng thái</span><b>Đang làm bài</b></div>
                    }
                </div>
                @if (!drawerStatus.Finished)
                {
                    <div style="font-size:12px;font-weight:700;color:var(--ink-soft);text-transform:uppercase;margin-bottom:6px;">Tiến độ từng câu</div>
                    <div class="qgrid">
                        @for (var i = 0; i < drawerStatus.Attempt!.Questions.Count; i++)
                        {
                            var index = i;
                            var answered = IsAnswered(drawerStatus.Attempt.Questions.ElementAt(index).Id);
                            <div class="qcell @(answered ? "answered" : "")">@(index + 1)</div>
                        }
                    </div>
                    <AppButton Variant="AppButtonVariant.Danger" FullWidth="true" OnClick="ForceSubmitAsync">Buộc nộp bài</AppButton>
                }
            }
        </div>
    </div>
}
```

- [ ] **Step 16: Build toàn solution**

Run: `dotnet build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 17: Commit (dùng `git add -A` để bao gồm cả file bị xoá)**

```bash
git add -A
git commit -m "feat: thay Fixed/Pool bằng ma trận cấu trúc đề thi và bỏ negative marking"
```

---

## Task 4: Xác minh toàn hệ thống, dọn dữ liệu Mongo, và kịch bản test tay đầu-cuối

**Files:** không tạo/sửa file — chỉ build, dọn dữ liệu, chạy stack và thao tác tay.

- [ ] **Step 1: Build toàn solution lần cuối**

Run: `dotnet build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 2: XÁC NHẬN với người dùng trước khi xoá dữ liệu**

Bước dọn dữ liệu bên dưới **xoá sạch mọi đề thi và mọi bài làm hiện có**. Hỏi và chờ người dùng đồng ý tường minh trước khi chạy Step 3 (theo mục Rủi ro của spec — không tự động xoá).

- [ ] **Step 3: Dọn dữ liệu MongoDB (chỉ chạy sau khi được duyệt)**

Đảm bảo container `mongo.db` đang chạy (`docker compose up -d mongodb`), rồi chạy (dùng đúng pattern mongosh của dự án):

```bash
docker exec mongo.db mongosh "mongodb://admin:Admin%4012345@localhost:27017/ExamDb?authSource=admin" --eval 'db.exams.drop(); db.examResults.drop(); db.rolePermissions.updateMany({}, { $pull: { permissions: { $in: ["Exam.ManageQuestions", "Exam.ManageNegativeMarking"] } } }); print("exams/examResults dropped, stale permissions pulled");'
```
Expected: in ra `exams/examResults dropped, stale permissions pulled` (giá trị `true`/`false` của `drop()` tuỳ collection có tồn tại hay không — cả hai đều chấp nhận được). Collection `questions` giữ nguyên.

Kiểm tra lại quyền rác đã sạch:
```bash
docker exec mongo.db mongosh "mongodb://admin:Admin%4012345@localhost:27017/ExamDb?authSource=admin" --eval 'printjson(db.rolePermissions.find({ permissions: { $in: ["Exam.ManageQuestions", "Exam.ManageNegativeMarking"] } }).toArray());'
```
Expected: `[]` (mảng rỗng).

- [ ] **Step 4: Rebuild và khởi động lại stack**

Run: `docker compose up -d --build identity.server exam.api exam.webapp`
Expected: 3 container start thành công; `docker compose ps` cho thấy `running`/`healthy`. (Lần khởi động này `DataSeeder` sẽ chỉ seed lại nếu `categories` rỗng — nếu muốn seed lại đề thi mẫu theo ma trận mới thì phải xoá thêm collection `categories`/`questions`; với mục đích test tay dưới đây, giảng viên sẽ tự tạo đề mới nên không bắt buộc.)

- [ ] **Step 5: Rà toàn bộ Admin không lỗi UI/console**

Đăng nhập Admin (`http://localhost:6004`), lần lượt vào `/admin/dashboard`, `/admin/categories`, `/admin/questions`, `/admin/exams`, `/admin/permissions`.
Expected: mọi trang render đúng, không có phần tử vỡ layout, không có lỗi trong Console trình duyệt (F12). Trang `/admin/questions` không còn cột "Điểm"; trang Phân quyền không còn 2 mục "Quản lý câu hỏi trong đề" và "Cấu hình trừ điểm".

- [ ] **Step 6: Tạo đề thi qua ma trận, publish (đăng nhập Instructor)**

Đăng nhập bằng tài khoản Instructor. Vào `/admin/questions`, chọn 1 môn học và đảm bảo ngân hàng có đủ câu theo ví dụ (nếu dùng seed mặc định, môn "Lập trình C#" có: 1 Dễ/Một đáp án, 1 Trung bình/Một đáp án, 1 Trung bình/Nhiều đáp án, 1 Khó/Một đáp án). Nếu ngân hàng thiếu, tạo thêm cho đủ.

Vào `/admin/exams`, chọn môn học đó, bấm "+ Thêm đề thi". Nhập tên, thời lượng, **Điểm đạt tối thiểu = 6.0**, và ma trận ví dụ cụ thể:
- Dễ / Một đáp án = **2**
- Trung bình / Một đáp án = **1**
- Trung bình / Nhiều đáp án = **1**

Tổng hiển thị = **4**. Lưu.
Expected: đề được tạo, mở chi tiết thấy "Số câu hỏi = 4"; card "Ma trận cấu trúc đề thi" hiển thị đúng các ô đã nhập. Bấm "Xuất bản".
Expected: trạng thái chuyển sang "Đã xuất bản" (không bị chặn vì `NumberOfQuestions = 4 > 0`).

- [ ] **Step 7: Làm bài với tài khoản Student, kiểm tra điểm đúng thang 0–10**

Đăng nhập bằng tài khoản Student (đề công khai nếu không gán lớp). Vào `/exams`, chọn đề vừa tạo, "Bắt đầu làm bài".
Expected: hiển thị đúng 4 câu, không còn dòng "… điểm" theo từng câu (đã bỏ Points).

Trả lời sao cho **đúng 3/4 câu**, "Nộp bài".
Expected: trang kết quả hiển thị điểm **`7.5/10 điểm`** (đúng công thức `3/4 × 10 = 7.5`), và vì `7.5 >= 6.0` nên hiển thị **"Đạt"**. Dòng "Số câu đúng: 3/4" khớp.

Làm lại 1 lượt khác trả lời đúng 2/4:
Expected: điểm **`5/10`**, `5 < 6.0` → **"Không đạt"**.

- [ ] **Step 8: Kiểm tra thông báo lỗi khi 1 ô ma trận thiếu câu trong ngân hàng**

Đăng nhập Instructor, tạo đề mới trong 1 môn học ít câu hỏi, với 1 ô yêu cầu nhiều hơn số câu thực có — ví dụ **Khó / Một đáp án = 5** trong khi môn đó chỉ có 1 câu Khó/Một đáp án. Lưu ma trận, publish.
Đăng nhập Student, "Bắt đầu làm bài" đề đó.
Expected: xuất hiện toast lỗi nêu rõ đúng ô đang thiếu, dạng: **"Không đủ câu hỏi mức 'Khó' loại 'Một đáp án' trong ngân hàng để rút 5 câu (chỉ có 1)."** — không phải màn hình lỗi chung chung, và không tạo được lượt thi.

- [ ] **Step 9: Xác nhận kết quả hiển thị phía Admin**

Đăng nhập lại Instructor (chủ đề thi ở Step 6), mở chi tiết đề, xem card "Kết quả thi".
Expected: các lượt đã nộp hiển thị điểm dạng `7.5/10`, `5/10`; cột Kết quả "Đạt"/"Không đạt" khớp; drawer chi tiết mở được, hiển thị điểm đúng.

- [ ] **Step 10: Không commit**

Task này không thay đổi mã nguồn nên không có commit. Mọi thay đổi code đã được commit ở Task 1–3.

---

## Ghi chú cho người duyệt (ngoài tài liệu kế hoạch)

Tôi đã đối chiếu từng mục 1–7 của phần "Thiết kế chi tiết" với các task; **tất cả đều có ít nhất một task phụ trách**, không có yêu cầu nào bị bỏ sót:
- §1 (Exam entity): Task 1 (`MinimumPassingScore` decimal) + Task 3 (xoá QuestionSelectionMode/Pool/NegativeMarking/QuestionIds, thêm `Composition`/`ConfigureComposition`/`NumberOfQuestions`).
- §2 (ExamQuestionPoolService rút theo ô): Task 3, Step 4 + method repo mới Step 3.
- §3 (bỏ Points khỏi Question/DTO/Request/UI/Excel): Task 2.
- §4 (chấm điểm ExamResult/QuestionResult/Grading): Task 2.
- §5 (UI): Task 2 (QuestionFormDialog, ImportQuestionsDialog, TakeExam) + Task 3 (ExamFormDialog, Exams.razor ma trận).
- §6 (API/Command/Permission): Task 3.
- §7 (Dữ liệu cũ): Task 4.

Một vài điểm quyết định tôi đã tự chốt trong lúc lập kế hoạch, xin nêu để anh nắm:
1. **`MaxPossibleScore` trong 3 DTO (`ExamResultDto`, `ExamResultSummaryDto`, `ExamResultAdminListItemDto`) đổi `int`→`decimal`** để khớp `ExamResult.MaxPossibleScore => 10m` mà không phải ép kiểu trong mapper. Binding hiển thị `@TotalScore/@MaxPossibleScore` không đổi (10m in ra "10"), đúng tinh thần spec là "không cần sửa gì phần hiển thị".
2. **`DataSeeder.cs` phải sửa** (spec không liệt kê tên file này nhưng nó gọi `AddQuestion`/`ConfigureNegativeMarking` nên bắt buộc đổi sang `ConfigureComposition` để build xanh) — đã đưa vào Task 3, dùng ma trận khớp phân bố câu hỏi seed (mọi môn seed đều có đúng 1 Dễ/Một đáp án, 1 Trung bình/Một đáp án, 1 Trung bình/Nhiều đáp án, 1 Khó/Một đáp án).
3. **`Permissions.razor.cs`** (WebApp) cũng phải bỏ 2 nhãn quyền — spec chỉ nhắc `Permissions.cs` backend, nhưng nếu để nguyên nhãn trỏ tới hằng số đã xoá thì không build được; đã đưa vào Task 3.
4. **Route đổi `/question-pool` → `/composition`** (spec để mở phần URL, chỉ khoá tên permission `ManagePool`). Nếu anh muốn giữ nguyên URL `/question-pool` để đỡ đụng client thì báo, tôi chỉnh lại Step 10/15 của Task 3.
5. **`ConfigureExamComposition` không kiểm tra số câu thực có trong ngân hàng tại thời điểm cấu hình** — lỗi thiếu câu theo từng ô cố ý để dồn về lúc `StartExam` (đúng spec §2 và kịch bản test Step 8). Nghĩa là giảng viên vẫn publish được đề mà ngân hàng chưa đủ câu; học viên sẽ gặp thông báo lỗi rõ ràng khi bắt đầu.