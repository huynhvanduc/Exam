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

    public async Task<IReadOnlyCollection<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, ApiRoutes.Categories.Base);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<CategoryDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<CategoryDto> CreateCategoryAsync(CategoryRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, ApiRoutes.Categories.Base, body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<CategoryDto> UpdateCategoryAsync(string id, CategoryRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Put, ApiRoutes.Categories.ById(id), body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteCategoryAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Delete, ApiRoutes.Categories.ById(id));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    public async Task<IReadOnlyCollection<QuestionDto>> GetQuestionsByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, ApiRoutes.Questions.ByCategory(categoryId));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<QuestionDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<QuestionDto> CreateQuestionAsync(QuestionRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, ApiRoutes.Questions.Base, body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<QuestionDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<QuestionDto> UpdateQuestionAsync(string id, QuestionRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Put, ApiRoutes.Questions.ById(id), body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<QuestionDto>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteQuestionAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Delete, ApiRoutes.Questions.ById(id));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    public async Task<IReadOnlyCollection<ExamDto>> GetExamsByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, ApiRoutes.Exams.ByCategory(categoryId));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<ExamDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<ExamDto> GetExamByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, ApiRoutes.Exams.ById(id));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExamDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<ExamDto> CreateExamAsync(ExamRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, ApiRoutes.Exams.Base, body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExamDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<ExamDto> UpdateExamAsync(string id, ExamRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Put, ApiRoutes.Exams.ById(id), body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExamDto>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteExamAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Delete, ApiRoutes.Exams.ById(id));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    public async Task<ExamDto> AddQuestionToExamAsync(string examId, string questionId, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, ApiRoutes.Exams.Question(examId, questionId));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExamDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<ExamDto> RemoveQuestionFromExamAsync(string examId, string questionId, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Delete, ApiRoutes.Exams.Question(examId, questionId));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExamDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<ExamDto> ConfigureQuestionPoolAsync(string examId, ConfigureQuestionPoolRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Put, ApiRoutes.Exams.QuestionPool(examId), body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExamDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<ExamDto> ScheduleExamAvailabilityAsync(string examId, ScheduleExamAvailabilityRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Put, ApiRoutes.Exams.Availability(examId), body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExamDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<ExamDto> ConfigureNegativeMarkingAsync(string examId, ConfigureNegativeMarkingRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Put, ApiRoutes.Exams.NegativeMarking(examId), body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExamDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<ExamDto> PublishExamAsync(string examId, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, ApiRoutes.Exams.Publish(examId));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExamDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<ExamDto> UnpublishExamAsync(string examId, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, ApiRoutes.Exams.Unpublish(examId));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExamDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<ExamDto> ArchiveExamAsync(string examId, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, ApiRoutes.Exams.Archive(examId));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExamDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<QuestionDto> GetQuestionByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, ApiRoutes.Questions.ById(id));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<QuestionDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<IReadOnlyCollection<RolePermissionDto>> GetRolePermissionsAsync(CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, ApiRoutes.RolePermissions.Base);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<RolePermissionDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<RolePermissionDto> UpdateRolePermissionsAsync(UserRole role, UpdateRolePermissionsRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Put, ApiRoutes.RolePermissions.ByRole(role), body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<RolePermissionDto>(cancellationToken: cancellationToken))!;
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
