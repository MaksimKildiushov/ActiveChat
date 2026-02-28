using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Ac.Admin.Infrastructure;

/// <summary>
/// Вызов API Ac.Auth для создания/получения пользователей (для тенантов и т.п.).
/// Запросы идут с Bearer-токеном текущего пользователя (OIDC access token).
/// </summary>
public interface IAuthApiClient
{
    /// <summary>
    /// Создаёт пользователя в Auth или возвращает Id существующего по email.
    /// </summary>
    Task<Guid?> CreateOrGetUserAsync(string email, string? password, string? displayName, string role, CancellationToken ct = default);
}

public sealed class AuthApiClient(
    HttpClient http,
    IConfiguration config,
    IHttpContextAccessor httpContextAccessor) : IAuthApiClient
{
    public async Task<Guid?> CreateOrGetUserAsync(string email, string? password, string? displayName, string role, CancellationToken ct = default)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null)
            return null;

        var authResult = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.Succeeded || authResult.Properties?.GetTokenValue("access_token") is not { } accessToken)
            return null;

        var baseUrl = (config["Infra:AuthBaseUrl"] ?? config["AuthForAdmin:Authority"])?.TrimEnd('/') ?? "https://localhost:7189";

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/invitations/create-user");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = JsonContent.Create(new { email, password, displayName, role });

        var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            return null;

        var body = await resp.Content.ReadFromJsonAsync<CreateUserResponse>(ct);
        return body?.UserId;
    }

    private sealed class CreateUserResponse
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }
    }
}
