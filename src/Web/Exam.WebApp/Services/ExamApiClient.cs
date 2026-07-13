using System.Net.Http.Headers;
using System.Net.Http.Json;
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

    public Task<ExamDto> AddQuestionToExamAsync(string examId, string questionId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Post, ApiRoutes.Exams.Question(examId, questionId), cancellationToken: cancellationToken);

    public Task<ExamDto> RemoveQuestionFromExamAsync(string examId, string questionId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Delete, ApiRoutes.Exams.Question(examId, questionId), cancellationToken: cancellationToken);

    public Task<ExamDto> ConfigureQuestionPoolAsync(string examId, ConfigureQuestionPoolRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Put, ApiRoutes.Exams.QuestionPool(examId), body, cancellationToken);

    public Task<ExamDto> ScheduleExamAvailabilityAsync(string examId, ScheduleExamAvailabilityRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Put, ApiRoutes.Exams.Availability(examId), body, cancellationToken);

    public Task<ExamDto> ConfigureNegativeMarkingAsync(string examId, ConfigureNegativeMarkingRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Put, ApiRoutes.Exams.NegativeMarking(examId), body, cancellationToken);

    public Task<ExamDto> PublishExamAsync(string examId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Post, ApiRoutes.Exams.Publish(examId), cancellationToken: cancellationToken);

    public Task<ExamDto> UnpublishExamAsync(string examId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Post, ApiRoutes.Exams.Unpublish(examId), cancellationToken: cancellationToken);

    public Task<ExamDto> ArchiveExamAsync(string examId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamDto>(HttpMethod.Post, ApiRoutes.Exams.Archive(examId), cancellationToken: cancellationToken);

    public Task<PagedResult<ExamResultAdminListItemDto>> GetExamResultsByExamAsync(string examId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<ExamResultAdminListItemDto>>(HttpMethod.Get, ApiRoutes.Exams.Results(examId, page, pageSize), cancellationToken: cancellationToken);

    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default) =>
        SendAsync<DashboardSummaryDto>(HttpMethod.Get, ApiRoutes.Dashboard.Summary, cancellationToken: cancellationToken);

    public Task<QuestionDto> GetQuestionByIdAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync<QuestionDto>(HttpMethod.Get, ApiRoutes.Questions.ById(id), cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<RolePermissionDto>> GetRolePermissionsAsync(CancellationToken cancellationToken = default) =>
        SendForCollectionAsync<RolePermissionDto>(HttpMethod.Get, ApiRoutes.RolePermissions.Base, cancellationToken: cancellationToken);

    public Task<RolePermissionDto> UpdateRolePermissionsAsync(UserRole role, UpdateRolePermissionsRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<RolePermissionDto>(HttpMethod.Put, ApiRoutes.RolePermissions.ByRole(role), body, cancellationToken);

    public Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<UserDto>>(HttpMethod.Get, ApiRoutes.Users.Paged(page, pageSize), cancellationToken: cancellationToken);

    public Task<UserDto> PromoteUserRoleAsync(string externalId, PromoteUserRoleRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<UserDto>(HttpMethod.Put, ApiRoutes.Users.Role(externalId), body, cancellationToken);

    public Task<PagedResult<AuditLogEntryDto>> GetAuditLogAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<AuditLogEntryDto>>(HttpMethod.Get, ApiRoutes.AuditLog.Paged(page, pageSize), cancellationToken: cancellationToken);

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

    public Task<ExamResultDto> GetExamResultAsync(string attemptId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamResultDto>(HttpMethod.Get, ApiRoutes.ExamAttempts.Result(attemptId), cancellationToken: cancellationToken);

    public Task<PagedResult<ExamResultSummaryDto>> GetMyExamHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<ExamResultSummaryDto>>(HttpMethod.Get, ApiRoutes.ExamAttempts.History(page, pageSize), cancellationToken: cancellationToken);

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

        var accessToken = await _httpContextAccessor.HttpContext!.GetTokenAsync("access_token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (body != null)
            request.Content = JsonContent.Create(body);

        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadAsStringAsync();
        throw new ExamApiException(response.StatusCode, problem);
    }
}

public class ExamApiException : Exception
{
    public System.Net.HttpStatusCode StatusCode { get; }

    public ExamApiException(System.Net.HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
