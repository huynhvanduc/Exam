using IdentityServer4.Models;

namespace Identity.API.Settings;

public static class Config
{
    public static IEnumerable<IdentityResource> GetIdentityResources()
    {
        return [
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        ];
    }

    public static IEnumerable<ApiScope> GetApiScopes(IdentityServerSettings settings)
    {
        return settings.ApiScopes.Select(s => new ApiScope(s.Name, s.DisplayName));
    }

    public static IEnumerable<ApiResource> GetApiResources(IdentityServerSettings settings)
    {
        return settings.ApiResources.Select(r => new ApiResource(r.Name, r.DisplayName)
        {
            Scopes = r.Scopes
        });
    }

    public static IEnumerable<Client> GetClients(IdentityServerSettings settings)
    {
        return settings.Clients.Select(c =>
        {
            var client = new Client
            {
                ClientId = c.ClientId,
                ClientName = c.ClientName,
                AllowedGrantTypes = MapGrantType(c.GrantType),
                RequirePkce = c.RequirePkce,
                RequireClientSecret = !string.IsNullOrEmpty(c.ClientSecret),
                AllowAccessTokensViaBrowser = c.AllowAccessTokensViaBrowser,
                AllowedScopes = c.AllowedScopes,
                RedirectUris = c.RedirectUris,
                PostLogoutRedirectUris = c.PostLogoutRedirectUris,
                RequireConsent = false
            };

            if (!string.IsNullOrEmpty(c.ClientSecret))
                client.ClientSecrets.Add(new Secret(c.ClientSecret.Sha256()));

            return client;
        });
    }

    private static ICollection<string> MapGrantType(string grantType) => grantType switch
    {
        "client_credentials" => GrantTypes.ClientCredentials,
        "authorization_code" => GrantTypes.Code,
        "hybrid" => GrantTypes.Hybrid,
        "implicit" => GrantTypes.Implicit,
        _ => throw new NotSupportedException($"Grant type '{grantType}' không được hỗ trợ.")
    };
}
