using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;

namespace Exam.WebApp.Services;

// Khớp đúng thứ tự Exam.Domain.Enums.UserRole (Exam.WebApp không reference trực tiếp assembly Domain của Exam.API).
public enum UserRole
{
    Student,
    Instructor,
    Admin
}

public record CurrentUserDto(string Id, string ExternalId, string FirstName, string LastName, UserRole Role);

public record CategoryDto(string Id, string Name, string UrlPath);

public record CreateCategoryRequest(string Name, string UrlPath);

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
        var request = await CreateRequestAsync(HttpMethod.Get, "/api/categories");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<CategoryDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, "/api/categories", body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<CategoryDto> UpdateCategoryAsync(string id, CreateCategoryRequest body, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Put, $"/api/categories/{id}", body);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteCategoryAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Delete, $"/api/categories/{id}");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
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
