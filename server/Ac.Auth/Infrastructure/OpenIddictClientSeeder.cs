using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Ac.Auth.Infrastructure;

/// <summary>
/// При старте приложения регистрирует OIDC-клиентов и scope'ы в OpenIddict (если ещё не заведены).
/// </summary>
public sealed class OpenIddictClientSeeder(
    IServiceProvider serviceProvider,
    IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        var authority = (configuration["Infra:AuthBaseUrl"] ?? configuration["OidcServer:Authority"])?.TrimEnd('/') ?? "https://localhost:7189";
        var adminClientId = configuration["OidcServer:AdminClientId"] ?? "Ac.Admin";
        var adminSecret = configuration["OidcServer:AdminClientSecret"] ?? "Ac.Admin-secret-change-in-production";
        var adminRedirectUri = configuration["OidcServer:AdminRedirectUri"] ?? "https://localhost:7011/signin-oidc";

        // Scope openid и profile
        if (await scopeManager.FindByNameAsync(Scopes.OpenId, cancellationToken) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = Scopes.OpenId,
                DisplayName = "OpenID Connect",
                Description = "OpenID Connect scope"
            }, cancellationToken);
        }

        if (await scopeManager.FindByNameAsync(Scopes.Profile, cancellationToken) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = Scopes.Profile,
                DisplayName = "Profile",
                Description = "Profile (name, etc.)"
            }, cancellationToken);
        }

        // Клиент Ac.Admin (confidential, authorization_code + client_credentials)
        if (await appManager.FindByClientIdAsync(adminClientId, cancellationToken) is null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = adminClientId,
                ClientSecret = adminSecret,
                DisplayName = "ActiveChat Admin",
                ConsentType = ConsentTypes.Implicit,
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.Prefixes.Scope + Scopes.OpenId,
                    Permissions.Prefixes.Scope + Scopes.Profile,
                    Permissions.ResponseTypes.Code
                },
                RedirectUris = { new Uri(adminRedirectUri) },
                PostLogoutRedirectUris = { new Uri(adminRedirectUri.Replace("/signin-oidc", "/")) },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange }
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
