using System.Net.Http.Headers;
using System.Security.Claims;
using Exam.WebApp.Components;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IAppToastService, AppToastService>();
builder.Services.AddScoped<IAppDialogService, AppDialogService>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

builder.Services.AddDataProtection()
    .SetApplicationName("Exam.WebApp")
    .PersistKeysToFileSystem(new DirectoryInfo("/app/dataprotection-keys"));

var identityAuthority = builder.Configuration["IdentityServer:Authority"]!;
var identityPublicAuthority = builder.Configuration["IdentityServer:PublicAuthority"] ?? identityAuthority;
var examApiBaseUrl = builder.Configuration["ExamApi:BaseUrl"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    // Mặc định ASP.NET Core trả về response trắng (403/404 rỗng) khi ForbidAsync/ChallengeAsync
    // được gọi mà không cấu hình đường dẫn - xảy ra ở lần load trang tĩnh đầu tiên (gõ thẳng URL),
    // trước khi Blazor Router kịp render NotAuthorized. Set 2 path này để luôn có redirect rõ ràng.
    options.AccessDeniedPath = "/access-denied";
    options.LoginPath = "/account/login";
})
.AddOpenIdConnect(options =>
{
    options.Authority = identityAuthority;
    options.ClientId = "exam.webapp";
    options.ResponseType = "code";
    options.UsePkce = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveTokens = true;

    // IdentityServer4 chỉ trả về id_token tối giản (sub/sid/auth_time...) cho response_type=code -
    // name/given_name/family_name/email nằm ở /connect/userinfo, không tự động có trong HttpContext.User
    // nếu không bật cờ này (mặc định false).
    options.GetClaimsFromUserInfoEndpoint = true;

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("exam_api.read");
    options.Scope.Add("exam_api.write");

    options.TokenValidationParameters.NameClaimType = "name";

    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            RewriteHost(context.ProtocolMessage, identityPublicAuthority);
            return Task.CompletedTask;
        },
        OnRedirectToIdentityProviderForSignOut = context =>
        {
            RewriteHost(context.ProtocolMessage, identityPublicAuthority);
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var accessToken = context.TokenEndpointResponse?.AccessToken;
            if (string.IsNullOrEmpty(accessToken) || context.Principal?.Identity is not ClaimsIdentity identity)
                return;

            try
            {
                var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client = httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(examApiBaseUrl);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.GetAsync(ApiRoutes.Users.Me);
                if (!response.IsSuccessStatusCode)
                    return;

                var user = await response.Content.ReadFromJsonAsync<UserDto>();
                if (user != null)
                    identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
            }
            catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("OpenIdConnect.OnTokenValidated");
                logger.LogWarning(ex, "Could not fetch user role from exam.api during login; proceeding without Role claim.");
            }
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler(options =>
{
    options.Retry.ShouldHandle = args =>
    {
        var isTransient = HttpClientResiliencePredicates.IsTransient(args.Outcome);
        var isSafeMethod = args.Context.GetRequestMessage()?.Method == HttpMethod.Get;
        return ValueTask.FromResult(isTransient && isSafeMethod);
    };
}));

builder.Services.AddHttpClient<ExamApiClient>(client =>
{
    client.BaseAddress = new Uri(examApiBaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();

// Bắt buộc UseRouting tường minh và đặt SAU UseStaticFiles: nếu không có UseRouting tường minh,
// ASP.NET Core tự chèn routing lên NGAY ĐẦU pipeline (trước UseStaticFiles) để phục vụ UseAuthorization/
// UseAntiforgery bên dưới - khi đó route catch-all "/{*pathInfo}" sẽ khớp và "chiếm" luôn các request
// file tĩnh (app.css, css/app-ui.css, js/shell.js...) trước khi StaticFileMiddleware kịp phục vụ, vì middleware
// này bỏ qua request đã có endpoint được gán. Đặt UseRouting ở đây đảm bảo static files luôn được ưu tiên.
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHealthChecks("/health").AllowAnonymous();

app.MapGet("/account/login", (string? returnUrl, HttpContext http) =>
    Results.Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
        [OpenIdConnectDefaults.AuthenticationScheme]));

app.MapPost("/account/logout", (HttpContext http) =>
    Results.SignOut(new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" },
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));

app.Run();

static void RewriteHost(Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage message, string publicAuthority)
{
    var publicUri = new Uri(publicAuthority);
    var target = new Uri(message.IssuerAddress);
    message.IssuerAddress = new UriBuilder(target)
    {
        Scheme = publicUri.Scheme,
        Host = publicUri.Host,
        Port = publicUri.Port
    }.Uri.ToString();
}
