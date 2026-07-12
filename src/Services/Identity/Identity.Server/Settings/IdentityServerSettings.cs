namespace Identity.Server.Settings;

public class IdentityServerSettings
{
    public List<ApiScopeSettings> ApiScopes { get; set; } = new();
    public List<ApiResourceSettings> ApiResources { get; set; } = new();
    public List<ClientSettings> Clients { get; set; } = new();
}

public class ApiScopeSettings
{
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
}

public class ApiResourceSettings
{
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public List<string> Scopes { get; set; } = new();
}

public class ClientSettings
{
    public string ClientId { get; set; } = default!;
    public string ClientName { get; set; } = default!;
    public string? ClientSecret { get; set; }
    public string GrantType { get; set; } = default!;
    public bool RequirePkce { get; set; }
    public bool AllowAccessTokensViaBrowser { get; set; }
    public List<string> AllowedScopes { get; set; } = new();
    public List<string> RedirectUris { get; set; } = new();
    public List<string> PostLogoutRedirectUris { get; set; } = new();
}
