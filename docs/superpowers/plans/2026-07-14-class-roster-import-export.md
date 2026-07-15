# Import/Export danh sách học viên trong lớp — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cho phép giảng viên/admin thêm hàng loạt học viên vào lớp bằng cách import file Excel 1 cột (Email), khớp vào tài khoản học viên đã tồn tại; và export danh sách thành viên hiện tại của lớp ra Excel.

**Architecture:** Mirror chính xác pattern Import/Export câu hỏi đã có (`ImportQuestionsCommand`/`ExportQuestionsQuery` dùng ClosedXML, dry-run trước khi ghi, báo lỗi từng dòng không chặn các dòng khác) sang aggregate `ClassRoom`. Thêm 1 method tra cứu hàng loạt theo email vào `IUserRepository`/`UserRepository` (mirror `GetByExternalIdsAsync`, dùng regex case-insensitive theo đúng pattern `BuildFilter` đã có). Domain `ClassRoom.AddMember` giữ nguyên (đã idempotent). Backend là CQRS/MediatR trên MongoDB; frontend là Blazor Server (Admin), dialog mirror `ImportQuestionsDialog`.

**Tech Stack:** .NET 8, MediatR/CQRS, FluentValidation, MongoDB (driver C#, ClosedXML cho Excel), Blazor Server (InteractiveServer).

## Global Constraints

- **Không viết unit test** (quy ước dự án). Xác minh bằng `dotnet build` cho toàn solution (Exam.API + Exam.WebApp) và thao tác tay trên stack chạy thực tế (đăng nhập vai Instructor/Admin, vào 1 lớp, import/export).
- **Import chỉ THÊM, không thay thế toàn bộ roster** — không xoá thành viên vắng mặt trong file.
- **So khớp email không phân biệt hoa/thường**, chuẩn hoá cả 2 phía (`Trim()` + so khớp không phân biệt hoa/thường) — đây là rủi ro chính nêu trong spec, phải nhất quán giữa tầng đọc file Excel và tầng query Mongo.
- **Không tạo permission mới** — dùng lại `Permissions.Class.ManageMembers` đã có sẵn và đã nằm trong `Permissions.All`.
- **Command chứa `byte[] FileContent` phải implement `ISkipAutoAuditLog`** (mirror `ImportQuestionsCommand`) để `AuditLoggingBehavior` không tự log payload base64 khổng lồ.
- MediatR handlers và FluentValidation validators được đăng ký tự động qua assembly scanning (`AddMediatR`/`AddValidatorsFromAssembly` trong `Exam.Application/ApplicationServiceCollectionExtensions.cs` và `Exam.Infrastructure/InfrastructureServiceCollectionExtensions.cs`) — **không cần đăng ký DI thủ công** cho handler/validator mới.
- Spec gốc: `docs/superpowers/specs/2026-07-14-class-roster-import-export-design.md`.

---

## Task 1: `IUserRepository.GetByEmailsAsync` — tra cứu hàng loạt theo email

**Files:**
- Modify: `src/Services/Exam/Exam.Domain/AggregateModels/UserAggregate/IUserRepository.cs` (thêm method mới sau `GetByExternalIdsAsync`, dòng 10)
- Modify: `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/Repositories/UserRepository.cs` (thêm implementation sau `GetByExternalIdsAsync`, dòng 24-29)

**Interfaces:**
- Produces: `IUserRepository.GetByEmailsAsync(IEnumerable<string> emails, CancellationToken) : Task<IReadOnlyCollection<User>>`. Task 3 (handler) dựa vào method này để tra cứu 1 lần cho toàn bộ danh sách email trong file.

- [ ] **Step 1: Thêm method vào interface**

Trong `src/Services/Exam/Exam.Domain/AggregateModels/UserAggregate/IUserRepository.cs`, sau dòng:
```csharp
    Task<IReadOnlyCollection<User>> GetByExternalIdsAsync(IEnumerable<string> externalIds, CancellationToken cancellationToken = default);
```
thêm ngay bên dưới:
```csharp

    Task<IReadOnlyCollection<User>> GetByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Build để xác nhận `UserRepository` chưa implement interface (lỗi biên dịch mong đợi)**

Run: `dotnet build src/Services/Exam/Exam.Infrastructure/Exam.Infrastructure.csproj`
Expected: FAIL — `'UserRepository' does not implement interface member 'IUserRepository.GetByEmailsAsync(...)'`

- [ ] **Step 3: Implement trong `UserRepository`**

Trong `src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/Repositories/UserRepository.cs`, sau method `GetByExternalIdsAsync` (dòng 24-29), thêm:
```csharp

    public async Task<IReadOnlyCollection<User>> GetByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default)
    {
        var normalizedEmails = (emails ?? Enumerable.Empty<string>())
            .Select(e => e?.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedEmails.Count == 0)
            return Array.Empty<User>();

        Logger.LogDebug("Getting Users by Emails {Emails}.", normalizedEmails);

        var filter = Builders<User>.Filter.Or(normalizedEmails
            .Select(email => Builders<User>.Filter.Regex(x => x.Email, new BsonRegularExpression($"^{Regex.Escape(email!)}$", "i"))));

        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }
```

Đây dùng lại đúng pattern `BsonRegularExpression` + `Regex.Escape` đã có trong `BuildFilter` (dòng 55-75 cùng file) để so khớp không phân biệt hoa/thường — `using System.Text.RegularExpressions;` và `using MongoDB.Bson;` đã có sẵn ở đầu file nên không cần thêm using.

- [ ] **Step 4: Build lại để xác nhận qua**

Run: `dotnet build src/Services/Exam/Exam.Infrastructure/Exam.Infrastructure.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add src/Services/Exam/Exam.Domain/AggregateModels/UserAggregate/IUserRepository.cs src/Services/Exam/Exam.Infrastructure/Persistence/Mongo/Repositories/UserRepository.cs
git commit -m "feat: thêm IUserRepository.GetByEmailsAsync tra cứu hàng loạt theo email"
```

---

## Task 2: Contracts — DTO cho Import/Export roster

**Files:**
- Create: `src/Services/Exam/Exam.Contracts/ImportClassMembersResultDto.cs`

**Interfaces:**
- Produces: `ImportClassMembersResultDto(int TotalRows, int AddedCount, int AlreadyMemberCount, IReadOnlyCollection<ImportClassMemberRowError> Errors, IReadOnlyCollection<ImportClassMemberPreviewRow> ValidRows)`, `ImportClassMemberRowError(int RowNumber, string Email, string Message)`, `ImportClassMemberPreviewRow(int RowNumber, string Email, string FullName, bool AlreadyMember)`. Task 3 (handler), Task 5 (controller), Task 6 (ExamApiClient), Task 7 (dialog) đều dùng các type này.

- [ ] **Step 1: Tạo file DTO**

Tạo `src/Services/Exam/Exam.Contracts/ImportClassMembersResultDto.cs`:
```csharp
namespace Exam.Contracts;

public record ImportClassMembersResultDto(
    int TotalRows,
    int AddedCount,
    int AlreadyMemberCount,
    IReadOnlyCollection<ImportClassMemberRowError> Errors,
    IReadOnlyCollection<ImportClassMemberPreviewRow> ValidRows);

public record ImportClassMemberRowError(int RowNumber, string Email, string Message);

public record ImportClassMemberPreviewRow(int RowNumber, string Email, string FullName, bool AlreadyMember);
```

- [ ] **Step 2: Build để xác nhận biên dịch qua**

Run: `dotnet build src/Services/Exam/Exam.Contracts/Exam.Contracts.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/Services/Exam/Exam.Contracts/ImportClassMembersResultDto.cs
git commit -m "feat: thêm DTO ImportClassMembersResultDto cho import roster lớp học"
```

---

## Task 3: Application — `ImportClassMembersCommand` + Handler + Validator

**Files:**
- Create: `src/Services/Exam/Exam.Application/ClassAggregate/Commands/ImportClassMembers/ImportClassMembersCommand.cs`
- Create: `src/Services/Exam/Exam.Application/ClassAggregate/Commands/ImportClassMembers/ImportClassMembersCommandHandler.cs`
- Create: `src/Services/Exam/Exam.Application/ClassAggregate/Commands/ImportClassMembers/ImportClassMembersCommandValidator.cs`

**Interfaces:**
- Consumes: `IClassRoomRepository.GetByIdAsync(string, CancellationToken) : Task<ClassRoom>`, `IClassRoomRepository.UpdateAsync(ClassRoom, CancellationToken) : Task`, `IUserRepository.GetByEmailsAsync(IEnumerable<string>, CancellationToken) : Task<IReadOnlyCollection<User>>` (Task 1), `OwnershipGuard.EnsureOwnerOrAdmin(Actor, string, string, string)`, `ClassRoom.AddMember(string)`, `ClassRoom.HasMember` không cần dùng trực tiếp — dùng `classRoom.MemberUserIds` để kiểm tra đã-là-thành-viên trước khi gọi `AddMember`. `Actor(string UserId, UserRole Role)` từ `Exam.Application.Common`.
- Produces: `ImportClassMembersCommand(string ClassId, byte[] FileContent, Actor Actor, bool DryRun) : IRequest<ImportClassMembersResultDto>, ISkipAutoAuditLog`. Task 5 (controller) gửi command này.

- [ ] **Step 1: Tạo Command**

Tạo `src/Services/Exam/Exam.Application/ClassAggregate/Commands/ImportClassMembers/ImportClassMembersCommand.cs`:
```csharp
using Exam.Application.Common;
using Exam.Contracts;
using MediatR;

namespace Exam.Application.ClassAggregate.Commands.ImportClassMembers;

// FileContent (byte[]) sẽ bị serialize thành chuỗi base64 khổng lồ nếu AuditLoggingBehavior tự log ->
// bỏ qua auto-log, kết quả import (số dòng thêm/lỗi) đã hiển thị đủ ở UI ngay sau khi gọi.
public record ImportClassMembersCommand(string ClassId, byte[] FileContent, Actor Actor, bool DryRun)
    : IRequest<ImportClassMembersResultDto>, ISkipAutoAuditLog;
```

- [ ] **Step 2: Tạo Validator**

Tạo `src/Services/Exam/Exam.Application/ClassAggregate/Commands/ImportClassMembers/ImportClassMembersCommandValidator.cs`:
```csharp
using FluentValidation;

namespace Exam.Application.ClassAggregate.Commands.ImportClassMembers;

public class ImportClassMembersCommandValidator : AbstractValidator<ImportClassMembersCommand>
{
    public ImportClassMembersCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.FileContent).NotEmpty().WithMessage("File is required.");
    }
}
```

- [ ] **Step 3: Tạo Handler**

Tạo `src/Services/Exam/Exam.Application/ClassAggregate/Commands/ImportClassMembers/ImportClassMembersCommandHandler.cs`:
```csharp
using ClosedXML.Excel;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Commands.ImportClassMembers;

public class ImportClassMembersCommandHandler : IRequestHandler<ImportClassMembersCommand, ImportClassMembersResultDto>
{
    private const int HeaderRowNumber = 1;

    private readonly IClassRoomRepository _classRoomRepository;
    private readonly IUserRepository _userRepository;

    public ImportClassMembersCommandHandler(IClassRoomRepository classRoomRepository, IUserRepository userRepository)
    {
        _classRoomRepository = classRoomRepository;
        _userRepository = userRepository;
    }

    public async Task<ImportClassMembersResultDto> Handle(ImportClassMembersCommand request, CancellationToken cancellationToken)
    {
        var classRoom = await _classRoomRepository.GetByIdAsync(request.ClassId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ClassRoom), request.ClassId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, classRoom.OwnerUserId, nameof(ClassRoom), classRoom.Id);

        using var workbook = new XLWorkbook(new MemoryStream(request.FileContent));
        var worksheet = workbook.Worksheets.First();
        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? HeaderRowNumber;

        var rowEmails = new List<(int RowNumber, string Email)>();
        var totalRows = 0;

        for (var rowNumber = HeaderRowNumber + 1; rowNumber <= lastRowNumber; rowNumber++)
        {
            var email = worksheet.Row(rowNumber).Cell(1).GetString().Trim();

            // Bỏ qua dòng trống hoàn toàn (thường gặp ở cuối file).
            if (string.IsNullOrWhiteSpace(email))
                continue;

            totalRows++;
            rowEmails.Add((rowNumber, email));
        }

        var users = await _userRepository.GetByEmailsAsync(rowEmails.Select(r => r.Email), cancellationToken);
        var userByEmail = users.ToDictionary(u => u.Email.Trim(), u => u, StringComparer.OrdinalIgnoreCase);

        var errors = new List<ImportClassMemberRowError>();
        var validRows = new List<ImportClassMemberPreviewRow>();
        var usersToAdd = new List<User>();

        foreach (var (rowNumber, email) in rowEmails)
        {
            if (!userByEmail.TryGetValue(email, out var user))
            {
                errors.Add(new ImportClassMemberRowError(rowNumber, email, "Không tìm thấy tài khoản với email này."));
                continue;
            }

            if (user.Role != UserRole.Student)
            {
                errors.Add(new ImportClassMemberRowError(rowNumber, email, "Tài khoản này không phải học viên."));
                continue;
            }

            var alreadyMember = classRoom.MemberUserIds.Contains(user.ExternalId);
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            validRows.Add(new ImportClassMemberPreviewRow(rowNumber, email, fullName, alreadyMember));

            if (!alreadyMember)
                usersToAdd.Add(user);
        }

        if (!request.DryRun && usersToAdd.Count > 0)
        {
            foreach (var user in usersToAdd)
                classRoom.AddMember(user.ExternalId);

            await _classRoomRepository.UpdateAsync(classRoom, cancellationToken);
        }

        var alreadyMemberCount = validRows.Count(r => r.AlreadyMember);

        return new ImportClassMembersResultDto(totalRows, usersToAdd.Count, alreadyMemberCount, errors, validRows);
    }
}
```

- [ ] **Step 4: Build để xác nhận biên dịch qua**

Run: `dotnet build src/Services/Exam/Exam.Application/Exam.Application.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add src/Services/Exam/Exam.Application/ClassAggregate/Commands/ImportClassMembers/
git commit -m "feat: thêm ImportClassMembersCommand/Handler/Validator"
```

---

## Task 4: Application — `ExportClassMembersQuery` + Handler

**Files:**
- Create: `src/Services/Exam/Exam.Application/ClassAggregate/Queries/ExportClassMembers/ExportClassMembersQuery.cs`
- Create: `src/Services/Exam/Exam.Application/ClassAggregate/Queries/ExportClassMembers/ExportClassMembersQueryHandler.cs`

**Interfaces:**
- Consumes: `IClassRoomRepository.GetByIdAsync`, `IUserRepository.GetByExternalIdsAsync` (đã có sẵn), `OwnershipGuard.EnsureOwnerOrAdmin`.
- Produces: `ExportClassMembersQuery(string ClassId, Actor Actor) : IRequest<byte[]>`. Task 5 (controller) gửi query này.

- [ ] **Step 1: Tạo Query**

Tạo `src/Services/Exam/Exam.Application/ClassAggregate/Queries/ExportClassMembers/ExportClassMembersQuery.cs`:
```csharp
using Exam.Application.Common;
using MediatR;

namespace Exam.Application.ClassAggregate.Queries.ExportClassMembers;

public record ExportClassMembersQuery(string ClassId, Actor Actor) : IRequest<byte[]>;
```

- [ ] **Step 2: Tạo Handler**

Tạo `src/Services/Exam/Exam.Application/ClassAggregate/Queries/ExportClassMembers/ExportClassMembersQueryHandler.cs`:
```csharp
using ClosedXML.Excel;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Queries.ExportClassMembers;

public class ExportClassMembersQueryHandler : IRequestHandler<ExportClassMembersQuery, byte[]>
{
    private static readonly string[] Headers = ["Họ tên", "Email"];

    private readonly IClassRoomRepository _classRoomRepository;
    private readonly IUserRepository _userRepository;

    public ExportClassMembersQueryHandler(IClassRoomRepository classRoomRepository, IUserRepository userRepository)
    {
        _classRoomRepository = classRoomRepository;
        _userRepository = userRepository;
    }

    public async Task<byte[]> Handle(ExportClassMembersQuery request, CancellationToken cancellationToken)
    {
        var classRoom = await _classRoomRepository.GetByIdAsync(request.ClassId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ClassRoom), request.ClassId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, classRoom.OwnerUserId, nameof(ClassRoom), classRoom.Id);

        var members = await _userRepository.GetByExternalIdsAsync(classRoom.MemberUserIds, cancellationToken);
        var memberById = members.ToDictionary(m => m.ExternalId);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Members");

        for (var i = 0; i < Headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = Headers[i];
        worksheet.Row(1).Style.Font.Bold = true;

        var rowNumber = 2;
        foreach (var userId in classRoom.MemberUserIds)
        {
            var fullName = memberById.TryGetValue(userId, out var user) ? $"{user.FirstName} {user.LastName}".Trim() : "(Không rõ)";
            var email = memberById.TryGetValue(userId, out var u) ? u.Email : string.Empty;

            worksheet.Cell(rowNumber, 1).Value = fullName;
            worksheet.Cell(rowNumber, 2).Value = email;
            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
```

- [ ] **Step 3: Build để xác nhận biên dịch qua**

Run: `dotnet build src/Services/Exam/Exam.Application/Exam.Application.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/Services/Exam/Exam.Application/ClassAggregate/Queries/ExportClassMembers/
git commit -m "feat: thêm ExportClassMembersQuery/Handler xuất Excel thành viên lớp"
```

---

## Task 5: API — endpoint Import/Export trên `ClassesController`

**Files:**
- Modify: `src/Services/Exam/Exam.API/Controllers/ClassesController.cs` (thêm 2 action ngay sau `RemoveMember`, dòng 79-85; thêm using cho 2 namespace mới)

**Interfaces:**
- Consumes: `ImportClassMembersCommand` (Task 3), `ExportClassMembersQuery` (Task 4), `User.GetActor()` (extension đã có sẵn).
- Produces: `POST /api/classes/{id}/members/import?dryRun={bool}` trả `ImportClassMembersResultDto`; `GET /api/classes/{id}/members/export` trả file `.xlsx`. Task 6 (ApiRoutes/ExamApiClient) gọi các route này.

- [ ] **Step 1: Thêm using**

Trong `src/Services/Exam/Exam.API/Controllers/ClassesController.cs`, đầu file, thêm 2 dòng using (theo thứ tự alphabet cùng khối using hiện có):
```csharp
using Exam.Application.ClassAggregate.Commands.ImportClassMembers;
using Exam.Application.ClassAggregate.Queries.ExportClassMembers;
```

- [ ] **Step 2: Thêm 2 action**

Trong `src/Services/Exam/Exam.API/Controllers/ClassesController.cs`, ngay sau method `RemoveMember` (kết thúc dòng 85 `}`), trước method `Join`, thêm:
```csharp

    [HttpPost("{id}/members/import")]
    [Authorize(Policy = Permissions.Class.ManageMembers)]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportMembers(string id, IFormFile file, [FromQuery] bool dryRun, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("File is required.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var result = await _mediator.Send(new ImportClassMembersCommand(id, stream.ToArray(), User.GetActor(), dryRun), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}/members/export")]
    [Authorize(Policy = Permissions.Class.ManageMembers)]
    public async Task<IActionResult> ExportMembers(string id, CancellationToken cancellationToken)
    {
        var bytes = await _mediator.Send(new ExportClassMembersQuery(id, User.GetActor()), cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "thanh-vien-lop.xlsx");
    }
```

- [ ] **Step 3: Build để xác nhận biên dịch qua**

Run: `dotnet build src/Services/Exam/Exam.API/Exam.API.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/Services/Exam/Exam.API/Controllers/ClassesController.cs
git commit -m "feat: thêm endpoint import/export thành viên lớp học"
```

---

## Task 6: WebApp — `ApiRoutes`/`ExamApiClient` cho Import/Export roster

**Files:**
- Modify: `src/Web/Exam.WebApp/Services/ApiRoutes.cs` (thêm 2 static method vào `Classes` class)
- Modify: `src/Web/Exam.WebApp/Services/ExamApiClient.cs` (thêm 2 method mới ngay sau `RemoveClassMemberAsync`)

**Interfaces:**
- Produces: `ApiRoutes.Classes.ImportMembers(string id, bool dryRun) : string`, `ApiRoutes.Classes.ExportMembers(string id) : string`, `ExamApiClient.ImportClassMembersAsync(string classId, Stream fileStream, string fileName, bool dryRun, CancellationToken) : Task<ImportClassMembersResultDto>`, `ExamApiClient.ExportClassMembersAsync(string classId, CancellationToken) : Task<byte[]>`. Task 7 (dialog) và Task 8 (Classes.razor) dùng các method này.

- [ ] **Step 1: Thêm route trong `ApiRoutes.cs`**

Trong `src/Web/Exam.WebApp/Services/ApiRoutes.cs`, trong class `Classes`, sau:
```csharp
    public static string Member(string id, string userId) => $"{Base}/{id}/members/{userId}";
```
thêm:
```csharp
    public static string ImportMembers(string id, bool dryRun) => $"{Base}/{id}/members/import?dryRun={dryRun}";
    public static string ExportMembers(string id) => $"{Base}/{id}/members/export";
```

- [ ] **Step 2: Thêm method trong `ExamApiClient.cs`**

Trong `src/Web/Exam.WebApp/Services/ExamApiClient.cs`, ngay sau method `RemoveClassMemberAsync`, thêm:
```csharp

    public async Task<ImportClassMembersResultDto> ImportClassMembersAsync(string classId, Stream fileStream, string fileName, bool dryRun, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Classes.ImportMembers(classId, dryRun)) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ImportClassMembersResultDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<byte[]> ExportClassMembersAsync(string classId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Classes.ExportMembers(classId));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
```

Method `GetAccessTokenAsync` và `EnsureSuccessAsync` đã có sẵn trong cùng file (dùng bởi `ImportQuestionsAsync`/`ExportQuestionsAsync`), không cần định nghĩa lại.

- [ ] **Step 3: Build để xác nhận biên dịch qua**

Run: `dotnet build src/Web/Exam.WebApp/Exam.WebApp.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/Web/Exam.WebApp/Services/ApiRoutes.cs src/Web/Exam.WebApp/Services/ExamApiClient.cs
git commit -m "feat: thêm ApiRoutes/ExamApiClient cho import/export thành viên lớp"
```

---

## Task 7: WebApp — `ImportClassMembersDialog.razor`

**Files:**
- Create: `src/Web/Exam.WebApp/Components/Pages/Admin/ImportClassMembersDialog.razor`
- Create: `src/Web/Exam.WebApp/Components/Pages/Admin/ImportClassMembersDialog.razor.cs`

**Interfaces:**
- Consumes: `ExamApiClient.ImportClassMembersAsync` (Task 6), `FormDialogBase` (`ExecuteAsync`, `Cancel`, `Dialog`), `ImportClassMembersResultDto`/`ImportClassMemberPreviewRow`/`ImportClassMemberRowError` (Task 2).
- Produces: dialog nhận `[Parameter] public string ClassId` (truyền vào khi mở dialog từ `Classes.razor`), trả về `ImportClassMembersResultDto` qua `Dialog.Close(AppDialogResult.Ok(result))` khi xác nhận. Task 8 (`Classes.razor`) mở dialog này và đọc kết quả trả về.

- [ ] **Step 1: Tạo `ImportClassMembersDialog.razor`**

Tạo `src/Web/Exam.WebApp/Components/Pages/Admin/ImportClassMembersDialog.razor`:
```razor
@inherits FormDialogBase
@using Microsoft.AspNetCore.Components.Forms

<p class="page-sub">
    Chọn file Excel (.xlsx) chỉ gồm 1 cột Email của các học viên đã có tài khoản trong hệ thống.
    Dùng nút "Export Excel" ở trang lớp học để lấy file mẫu đúng định dạng.
</p>

<InputFile OnChange="OnFileSelectedAsync" accept=".xlsx" />

@if (isBusy && previewResult == null)
{
    <AppSpinner />
}

@if (previewResult != null)
{
    <p style="margin:12px 0;">
        Tổng số dòng: <b>@previewResult.TotalRows</b> — Sẽ thêm mới: <b>@previewResult.AddedCount</b> — Đã là thành viên: <b>@previewResult.AlreadyMemberCount</b> — Lỗi: <b>@previewResult.Errors.Count</b>
    </p>

    @if (previewResult.ValidRows.Count > 0)
    {
        <div style="font-size:13px;font-weight:700;margin-bottom:6px;">Danh sách hợp lệ:</div>
        <div style="max-height:260px;overflow-y:auto;">
            <table class="data">
                <thead><tr><th>Dòng</th><th>Email</th><th>Họ tên</th><th>Trạng thái</th></tr></thead>
                <tbody>
                    @foreach (var row in previewResult.ValidRows.Take(MaxPreviewRows))
                    {
                        <tr>
                            <td>@row.RowNumber</td>
                            <td>@row.Email</td>
                            <td>@row.FullName</td>
                            <td>@(row.AlreadyMember ? "Đã là thành viên" : "Sẽ thêm")</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
        @if (previewResult.ValidRows.Count > MaxPreviewRows)
        {
            <p class="page-sub">… và @(previewResult.ValidRows.Count - MaxPreviewRows) dòng hợp lệ khác.</p>
        }
    }

    @if (previewResult.Errors.Count > 0)
    {
        <AppAlert Severity="AppAlertSeverity.Warning">
            @previewResult.Errors.Count dòng lỗi sẽ bị BỎ QUA khi xác nhận nhập, các dòng còn lại vẫn được thêm bình thường.
        </AppAlert>
        <div style="max-height:200px;overflow-y:auto;">
            <table class="data">
                <thead><tr><th>Dòng</th><th>Email</th><th>Lỗi</th></tr></thead>
                <tbody>
                    @foreach (var error in previewResult.Errors.Take(MaxPreviewRows))
                    {
                        <tr>
                            <td>@error.RowNumber</td>
                            <td>@error.Email</td>
                            <td>@error.Message</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
        @if (previewResult.Errors.Count > MaxPreviewRows)
        {
            <p class="page-sub">… và @(previewResult.Errors.Count - MaxPreviewRows) lỗi khác.</p>
        }
    }
}

<div class="modal-actions">
    <AppButton OnClick="Cancel">Huỷ</AppButton>
    @if (previewResult != null)
    {
        <AppButton Variant="AppButtonVariant.Primary" Disabled="previewResult.AddedCount == 0 || isBusy" OnClick="ConfirmAsync">
            Xác nhận nhập (@previewResult.AddedCount học viên)
        </AppButton>
    }
</div>
```

- [ ] **Step 2: Tạo `ImportClassMembersDialog.razor.cs`**

Tạo `src/Web/Exam.WebApp/Components/Pages/Admin/ImportClassMembersDialog.razor.cs`:
```csharp
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
```

- [ ] **Step 3: Build để xác nhận biên dịch qua**

Run: `dotnet build src/Web/Exam.WebApp/Exam.WebApp.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/Web/Exam.WebApp/Components/Pages/Admin/ImportClassMembersDialog.razor src/Web/Exam.WebApp/Components/Pages/Admin/ImportClassMembersDialog.razor.cs
git commit -m "feat: thêm ImportClassMembersDialog xem trước + xác nhận nhập roster"
```

---

## Task 8: WebApp — nút Import/Export trên `Classes.razor`

**Files:**
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor` (thêm 2 nút trong card "Thành viên", trước dòng `<h3>Thành viên (@selected.Members.Count)</h3>`)
- Modify: `src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor.cs` (thêm `[Inject] IJSRuntime JS`, `OpenImportMembersDialogAsync`, `ExportMembersAsync`)

**Interfaces:**
- Consumes: `ShowFormDialogAsync<ImportClassMembersDialog, ImportClassMembersResultDto>` (từ `AdminPageBase`, Task 7), `Api.ExportClassMembersAsync` (Task 6), `CanManage(currentUser, selected.OwnerUserId)` (đã có sẵn ở `AdminPageBase`), `Api.GetClassByIdAsync` (đã có sẵn).

- [ ] **Step 1: Thêm nút trong `Classes.razor`**

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor`, thay:
```razor
<AppCard>
    <h3>Thành viên (@selected.Members.Count)</h3>
```
bằng:
```razor
<AppCard>
    <div class="toolbar" style="margin-bottom:8px;">
        <h3 style="margin:0;">Thành viên (@selected.Members.Count)</h3>
        <span class="spacer"></span>
        <AppButton Disabled="!CanManage(currentUser, selected.OwnerUserId)" OnClick="OpenImportMembersDialogAsync">Import Excel</AppButton>
        <AppButton Disabled="!CanManage(currentUser, selected.OwnerUserId)" OnClick="ExportMembersAsync">Export Excel</AppButton>
    </div>
```

- [ ] **Step 2: Thêm inject + 2 method trong `Classes.razor.cs`**

Trong `src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor.cs`, thêm using và inject ngay dưới khai báo class:
```csharp
using Microsoft.JSInterop;
```
Thêm dòng inject ngay sau `[Parameter] public string? Id { get; set; }`:
```csharp
    [Inject] private IJSRuntime JS { get; set; } = null!;
```

Sau method `RemoveMemberAsync` (cuối file), thêm:
```csharp

    private async Task OpenImportMembersDialogAsync()
    {
        // Dialog tự lo bước Xem trước (dry-run) + Xác nhận nhập bên trong nó, chỉ đóng lại và trả về
        // kết quả cuối cùng SAU KHI admin đã xác nhận (Cancel nếu admin bỏ ngang ở bất kỳ bước nào).
        var parameters = new Dictionary<string, object> { ["ClassId"] = selected!.Id };
        var result = await ShowFormDialogAsync<ImportClassMembersDialog, ImportClassMembersResultDto>("Nhập danh sách học viên từ Excel", parameters);
        if (result == null)
            return;

        var message = result.AddedCount > 0
            ? $"Đã thêm {result.AddedCount} học viên, {result.AlreadyMemberCount} đã là thành viên sẵn, {result.Errors.Count} lỗi."
            : $"Không có học viên nào được thêm ({result.TotalRows} dòng, {result.AlreadyMemberCount} đã là thành viên, {result.Errors.Count} lỗi).";

        Toast.Add(message, result.Errors.Count == 0 ? AppSeverity.Success : AppSeverity.Warning);

        selected = await Api.GetClassByIdAsync(selected.Id);
        await LoadListAsync();
    }

    private Task ExportMembersAsync() => ExecuteAsync(async () =>
    {
        var bytes = await Api.ExportClassMembersAsync(selected!.Id);
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("downloadFileFromBytes", $"thanh-vien-lop-{selected.Id}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", base64);
    }, "Xuất Excel thất bại");
```

- [ ] **Step 3: Build để xác nhận biên dịch qua**

Run: `dotnet build src/Web/Exam.WebApp/Exam.WebApp.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor src/Web/Exam.WebApp/Components/Pages/Admin/Classes.razor.cs
git commit -m "feat: thêm nút Import/Export Excel cho thành viên lớp học"
```

---

## Task 9: Xác minh thủ công end-to-end

**Files:** (không sửa code — chỉ thao tác tay trên stack chạy thực tế)

**Interfaces:** không có.

- [ ] **Step 1: Build toàn solution**

Run: `dotnet build`
Expected: Build succeeded, 0 Error.

- [ ] **Step 2: Chuẩn bị dữ liệu test**

Đăng nhập vai Instructor/Admin sở hữu ít nhất 1 lớp. Đảm bảo có sẵn: 2 tài khoản Student hợp lệ (email X, Y), 1 tài khoản Instructor/Admin (email Z, dùng để test lỗi "không phải học viên"), và lớp đã có sẵn 1 thành viên (email W, dùng để test "đã là thành viên").

- [ ] **Step 3: Test Export trước (lấy file mẫu)**

Vào trang chi tiết lớp → bấm "Export Excel" → xác nhận file `.xlsx` tải về có cột "Họ tên"/"Email" đúng dữ liệu thành viên hiện tại (kể cả khi lớp chưa có thành viên nào, vẫn tải được file chỉ có header).

- [ ] **Step 4: Test Import — trộn 4 loại dòng trong 1 file**

Sửa file export (hoặc tạo file mới) với cột Email gồm 4 dòng: email X (hợp lệ, chưa là thành viên), email không tồn tại bất kỳ, email Z (Instructor/Admin), email W (đã là thành viên). Bấm "Import Excel" → chọn file → xác nhận dialog hiển thị đúng:
- Tổng dòng = 4, Sẽ thêm mới = 1 (X), Đã là thành viên = 1 (W), Lỗi = 2 (không tìm thấy + không phải học viên).
- Bấm "Xác nhận nhập" → toast hiển thị đúng số liệu → danh sách "Thành viên (N)" cập nhật ngay, thêm X, không có Y (chưa nằm trong file này), Z không xuất hiện.

- [ ] **Step 5: Test idempotent — import lại đúng file đó lần 2**

Import lại chính file ở Step 4 → xác nhận X giờ rơi vào nhóm "Đã là thành viên" (không lỗi, không thêm trùng) → số lượng thành viên lớp không đổi so với sau Step 4.

- [ ] **Step 6: Test quyền hạn**

Đăng nhập bằng tài khoản Instructor khác (không sở hữu lớp này) → xác nhận nút "Import Excel"/"Export Excel" bị khoá (Disabled) giống nút "Xoá" hiện có, theo `CanManage`.

- [ ] **Step 7: Commit ghi chú xác minh (nếu có thay đổi phát sinh khi sửa lỗi trong lúc test)**

Nếu Step 4-6 phát hiện lỗi cần sửa code, sửa trực tiếp trong task tương ứng ở trên rồi lặp lại từ Step 4. Nếu không có thay đổi nào, task này không cần commit.
