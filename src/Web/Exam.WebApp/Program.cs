using System.Net.Http.Headers;
using System.Security.Claims;
using Exam.WebApp.Components;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Snackbar ở góc dưới-phải để không đè lên các nút hành động (Xuất bản/Lưu trữ...) đặt ở góc trên-phải các trang admin.
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

var identityAuthority = builder.Configuration["IdentityServer:Authority"]!;
// Trong Docker, backend gọi Identity qua tên container (identity.server:8080) nhưng trình duyệt
// phải redirect tới địa chỉ public (localhost:5001) vì "identity.server" không resolve được từ máy host.
var identityPublicAuthority = builder.Configuration["IdentityServer:PublicAuthority"] ?? identityAuthority;
var examApiBaseUrl = builder.Configuration["ExamApi:BaseUrl"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = identityAuthority;
    options.ClientId = "exam.webapp";
    options.ResponseType = "code";
    options.UsePkce = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveTokens = true;

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
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
            // Role sống trong Exam.Domain.User (Exam.API), không nằm trong JWT do Identity Server phát hành.
            // Gọi /api/users/me 1 lần lúc đăng nhập để lấy Role, gắn vào cookie claims cho cả phiên làm việc.
            var accessToken = context.TokenEndpointResponse?.AccessToken;
            if (string.IsNullOrEmpty(accessToken) || context.Principal?.Identity is not ClaimsIdentity identity)
                return;

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
    };
});

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<ExamApiClient>(client =>
{
    client.BaseAddress = new Uri(examApiBaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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
