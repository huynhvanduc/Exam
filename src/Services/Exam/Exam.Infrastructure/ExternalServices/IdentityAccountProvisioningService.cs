using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;
using Microsoft.Extensions.Configuration;

namespace Exam.Infrastructure.ExternalServices;

public class IdentityAccountProvisioningService : IIdentityAccountProvisioningService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _scope;

    public IdentityAccountProvisioningService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _clientId = configuration["IdentityServer:InternalClientId"]!;
        _clientSecret = configuration["IdentityServer:InternalClientSecret"]!;
        _scope = configuration["IdentityServer:InternalScope"]!;
    }

    public async Task<ProvisionedAccount> CreateAccountAsync(string email, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var accessToken = await RequestAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/accounts")
        {
            Content = JsonContent.Create(new { email, firstName, lastName })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new ExamDomainException("Email đã được sử dụng cho tài khoản khác.");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AccountCreatedResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Identity.Server trả về phản hồi rỗng khi tạo tài khoản.");

        return new ProvisionedAccount(result.ExternalId, result.GeneratedPassword);
    }

    private async Task<string> RequestAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["scope"] = _scope
            })
        };

        using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Không lấy được access token từ Identity.Server.");

        return token.AccessToken;
    }

    private record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
    private record AccountCreatedResponse(string ExternalId, string GeneratedPassword);
}
