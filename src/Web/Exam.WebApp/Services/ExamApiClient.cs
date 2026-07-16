using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Exam.WebApp.Services;

public class ExamApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExamApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<UserDto> GetMeAsync(CancellationToken cancellationToken = default) =>
        SendAsync<UserDto>(HttpMethod.Get, ApiRoutes.Users.Me, cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        SendForCollectionAsync<CategoryDto>(HttpMethod.Get, ApiRoutes.Categories.Base, cancellationToken: cancellationToken);

    public Task<CategoryDto> CreateCategoryAsync(CategoryRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<CategoryDto>(HttpMethod.Post, ApiRoutes.Categories.Base, body, cancellationToken);

    public Task<CategoryDto> UpdateCategoryAsync(string id, CategoryRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<CategoryDto>(HttpMethod.Put, ApiRoutes.Categories.ById(id), body, cancellationToken);

    public Task DeleteCategoryAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, ApiRoutes.Categories.ById(id), cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<QuestionDto>> GetQuestionsByCategoryAsync(string categoryId, CancellationToken cancellationToken = default) =>
        SendForCollectionAsync<QuestionDto>(HttpMethod.Get, ApiRoutes.Questions.ByCategory(categoryId), cancellationToken: cancellationToken);

    public Task<PagedResult<QuestionDto>> GetQuestionsByCategoryPagedAsync(string categoryId, int page, int pageSize,
        Level? level = null, QuestionType? questionType = null, string? keyword = null, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<QuestionDto>>(HttpMethod.Get, ApiRoutes.Questions.ByCategoryPaged(categoryId, page, pageSize, level, questionType, keyword), cancellationToken: cancellationToken);

    public Task<QuestionDto> CreateQuestionAsync(QuestionRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<QuestionDto>(HttpMethod.Post, ApiRoutes.Questions.Base, body, cancellationToken);

    public Task<QuestionDto> UpdateQuestionAsync(string id, QuestionRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<QuestionDto>(HttpMethod.Put, ApiRoutes.Questions.ById(id), body, cancellationToken);

    public Task DeleteQuestionAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, ApiRoutes.Questions.ById(id), cancellationToken: cancellationToken);

    public Task MoveQuestionsAsync(MoveQuestionsRequest body, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, ApiRoutes.Questions.Move, body, cancellationToken);

    public async Task<ImportQuestionsResultDto> ImportQuestionsAsync(Stream fileStream, string fileName, bool dryRun, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Questions.Import(dryRun)) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ImportQuestionsResultDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<byte[]> ExportQuestionsAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Questions.Export(categoryId));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public Task<PagedResult<ExamDto>> GetExamsByCategoryAsync(string categoryId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<ExamDto>>(HttpMethod.Get, ApiRoutes.Exams.ByCategory(categoryId, page, pageSize), cancellationToken: cancellationToken);

    public Task<ExamDto> GetExamByIdAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Get, ApiRoutes.Exams.ById(id), cancellationToken: cancellationToken);

    public Task<ExamDto> CreateExamAsync(ExamRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Post, ApiRoutes.Exams.Base, body, cancellationToken);

    public Task<ExamDto> UpdateExamAsync(string id, ExamRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Put, ApiRoutes.Exams.ById(id), body, cancellationToken);

    public Task DeleteExamAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, ApiRoutes.Exams.ById(id), cancellationToken: cancellationToken);

    public Task<ExamDto> ConfigureExamCompositionAsync(string examId, ConfigureExamCompositionRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Put, ApiRoutes.Exams.Composition(examId), body, cancellationToken);

    public Task<ExamDto> ScheduleExamAvailabilityAsync(string examId, ScheduleExamAvailabilityRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Put, ApiRoutes.Exams.Availability(examId), body, cancellationToken);

    public Task<ExamDto> ConfigureMaxAttemptsAsync(string examId, ConfigureMaxAttemptsRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Put, ApiRoutes.Exams.MaxAttempts(examId), body, cancellationToken);

    public Task<ExamDto> PublishExamAsync(string examId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Post, ApiRoutes.Exams.Publish(examId), cancellationToken: cancellationToken);

    public Task<ExamDto> UnpublishExamAsync(string examId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Post, ApiRoutes.Exams.Unpublish(examId), cancellationToken: cancellationToken);

    public Task<ExamDto> ArchiveExamAsync(string examId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Post, ApiRoutes.Exams.Archive(examId), cancellationToken: cancellationToken);

    public Task<PagedResult<ExamResultAdminListItemDto>> GetExamResultsByExamAsync(string examId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<ExamResultAdminListItemDto>>(HttpMethod.Get, ApiRoutes.Exams.Results(examId, page, pageSize), cancellationToken: cancellationToken);

    public async Task<byte[]> ExportExamResultsAsync(string examId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Exams.ExportResults(examId));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<ClassMemberDto>> GetExamNotAttemptedMembersAsync(string examId, CancellationToken cancellationToken = default) =>
        SendForCollectionAsync<ClassMemberDto>(HttpMethod.Get, ApiRoutes.Exams.NotAttempted(examId), cancellationToken: cancellationToken);

    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default) =>
        SendAsync<DashboardSummaryDto>(HttpMethod.Get, ApiRoutes.Dashboard.Summary, cancellationToken: cancellationToken);

    public Task<QuestionDto> GetQuestionByIdAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync<QuestionDto>(HttpMethod.Get, ApiRoutes.Questions.ById(id), cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<RolePermissionDto>> GetRolePermissionsAsync(CancellationToken cancellationToken = default) =>
        SendForCollectionAsync<RolePermissionDto>(HttpMethod.Get, ApiRoutes.RolePermissions.Base, cancellationToken: cancellationToken);

    public Task<RolePermissionDto> UpdateRolePermissionsAsync(UserRole role, UpdateRolePermissionsRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<RolePermissionDto>(HttpMethod.Put, ApiRoutes.RolePermissions.ByRole(role), body, cancellationToken);

    public Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search = null, UserRole? role = null,
        bool? isActive = null, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<UserDto>>(HttpMethod.Get, ApiRoutes.Users.Paged(page, pageSize, search, role, isActive), cancellationToken: cancellationToken);

    public Task<UserDto> PromoteUserRoleAsync(string externalId, PromoteUserRoleRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<UserDto>(HttpMethod.Put, ApiRoutes.Users.Role(externalId), body, cancellationToken);

    public Task<UserDto> ToggleUserActiveAsync(string externalId, ToggleUserActiveRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<UserDto>(HttpMethod.Put, ApiRoutes.Users.Active(externalId), body, cancellationToken);

    public Task<PagedResult<AuditLogEntryDto>> GetAuditLogAsync(int page, int pageSize, string? actor = null, string? action = null,
        DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<AuditLogEntryDto>>(HttpMethod.Get, ApiRoutes.AuditLog.Paged(page, pageSize, actor, action, from, to), cancellationToken: cancellationToken);

    public Task<PagedResult<ExamDto>> GetAvailableExamsAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<ExamDto>>(HttpMethod.Get, ApiRoutes.Exams.Available(page, pageSize), cancellationToken: cancellationToken);

    public Task<ExamAttemptDto> StartExamAsync(string examId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamAttemptDto>(HttpMethod.Post, ApiRoutes.ExamAttempts.Start, new StartExamRequest(examId), cancellationToken);

    public Task<ExamAttemptStatusDto> GetExamAttemptStatusAsync(string attemptId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamAttemptStatusDto>(HttpMethod.Get, ApiRoutes.ExamAttempts.ById(attemptId), cancellationToken: cancellationToken);

    public Task<RecordAnswerResultDto> RecordAnswerAsync(string attemptId, string questionId, IReadOnlyCollection<string> selectedAnswerIds, CancellationToken cancellationToken = default) =>
        SendAsync<RecordAnswerResultDto>(HttpMethod.Post, ApiRoutes.ExamAttempts.Answers(attemptId), new RecordAnswerRequest(questionId, selectedAnswerIds), cancellationToken);

    public Task<ExamResultDto> FinishExamAsync(string attemptId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamResultDto>(HttpMethod.Post, ApiRoutes.ExamAttempts.Finish(attemptId), cancellationToken: cancellationToken);

    public Task<ExamAttemptStatusDto> GetExamAttemptAdminStatusAsync(string attemptId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamAttemptStatusDto>(HttpMethod.Get, ApiRoutes.ExamAttempts.AdminStatus(attemptId), cancellationToken: cancellationToken);

    public Task<ExamResultDto> AdminForceFinishExamAsync(string attemptId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamResultDto>(HttpMethod.Post, ApiRoutes.ExamAttempts.AdminForceFinish(attemptId), cancellationToken: cancellationToken);

    public Task<ExamResultDto> AdminRegradeExamAsync(string attemptId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamResultDto>(HttpMethod.Post, ApiRoutes.ExamAttempts.AdminRegrade(attemptId), cancellationToken: cancellationToken);

    public Task<ExamResultDto> GetExamResultAsync(string attemptId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamResultDto>(HttpMethod.Get, ApiRoutes.ExamAttempts.Result(attemptId), cancellationToken: cancellationToken);

    public Task<PagedResult<ExamResultSummaryDto>> GetMyExamHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<ExamResultSummaryDto>>(HttpMethod.Get, ApiRoutes.ExamAttempts.History(page, pageSize), cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<ClassRoomDto>> GetClassesAsync(CancellationToken cancellationToken = default) =>
        SendForCollectionAsync<ClassRoomDto>(HttpMethod.Get, ApiRoutes.Classes.Base, cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<ClassRoomDto>> GetMyClassesAsync(CancellationToken cancellationToken = default) =>
        SendForCollectionAsync<ClassRoomDto>(HttpMethod.Get, ApiRoutes.Classes.Mine, cancellationToken: cancellationToken);

    public Task<ClassRoomDetailDto> GetClassByIdAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync<ClassRoomDetailDto>(HttpMethod.Get, ApiRoutes.Classes.ById(id), cancellationToken: cancellationToken);

    public Task<ClassRoomDto> CreateClassAsync(CreateClassRoomRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ClassRoomDto>(HttpMethod.Post, ApiRoutes.Classes.Base, body, cancellationToken);

    public Task<ClassRoomDto> RenameClassAsync(string id, RenameClassRoomRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ClassRoomDto>(HttpMethod.Put, ApiRoutes.Classes.ById(id), body, cancellationToken);

    public Task DeleteClassAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, ApiRoutes.Classes.ById(id), cancellationToken: cancellationToken);

    public Task<ClassRoomDto> RegenerateJoinCodeAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync<ClassRoomDto>(HttpMethod.Post, ApiRoutes.Classes.RegenerateCode(id), cancellationToken: cancellationToken);

    public Task<ClassRoomDetailDto> RemoveClassMemberAsync(string id, string userId, CancellationToken cancellationToken = default) =>
        SendAsync<ClassRoomDetailDto>(HttpMethod.Delete, ApiRoutes.Classes.Member(id, userId), cancellationToken: cancellationToken);

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

    public Task<ClassRoomDto> JoinClassAsync(string joinCode, CancellationToken cancellationToken = default) =>
        SendAsync<ClassRoomDto>(HttpMethod.Post, ApiRoutes.Classes.Join, new JoinClassRequest(joinCode), cancellationToken);

    public Task<ExamDto> AssignExamToClassAsync(string examId, string classId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Post, ApiRoutes.Exams.Class(examId, classId), cancellationToken: cancellationToken);

    public Task<ExamDto> UnassignExamFromClassAsync(string examId, string classId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Delete, ApiRoutes.Exams.Class(examId, classId), cancellationToken: cancellationToken);

    private async Task<TResponse> SendAsync<TResponse>(HttpMethod method, string url, object? body = null, CancellationToken cancellationToken = default)
    {
        var response = await SendCoreAsync(method, url, body, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken))!;
    }

    private async Task<IReadOnlyCollection<TItem>> SendForCollectionAsync<TItem>(HttpMethod method, string url, object? body = null, CancellationToken cancellationToken = default)
    {
        var response = await SendCoreAsync(method, url, body, cancellationToken);
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<TItem>>(cancellationToken: cancellationToken) ?? [];
    }

    private async Task SendAsync(HttpMethod method, string url, object? body = null, CancellationToken cancellationToken = default) =>
        await SendCoreAsync(method, url, body, cancellationToken);

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        var request = await CreateRequestAsync(method, url, body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return response;
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        if (body != null)
            request.Content = JsonContent.Create(body);

        return request;
    }

    private Task<string?> GetAccessTokenAsync() => _httpContextAccessor.HttpContext!.GetTokenAsync("access_token");

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadAsStringAsync();
        throw new ExamApiException(response.StatusCode, ExtractMessage(problem));
    }

    // Backend (GlobalExceptionHandler) luôn trả lỗi dạng ProblemDetails JSON - lấy đúng "detail" (hoặc
    // "errors" cho lỗi validate, hoặc "title" khi không có gì khác) thay vì để nguyên JSON thô rơi thẳng
    // vào toast, khiến người dùng thấy cả {"title":...,"status":...} thay vì câu message dễ hiểu.
    private static string ExtractMessage(string problemJson)
    {
        try
        {
            var problem = JsonSerializer.Deserialize<ApiProblemDetails>(problemJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (problem == null)
                return problemJson;

            if (!string.IsNullOrWhiteSpace(problem.Detail))
                return problem.Detail;

            if (problem.Errors is { Count: > 0 })
                return string.Join(" ", problem.Errors.SelectMany(e => e.Value));

            return !string.IsNullOrWhiteSpace(problem.Title) ? problem.Title : problemJson;
        }
        catch (JsonException)
        {
            return problemJson;
        }
    }

    private sealed record ApiProblemDetails(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}

public class ExamApiException : Exception
{
    public System.Net.HttpStatusCode StatusCode { get; }

    public ExamApiException(System.Net.HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
