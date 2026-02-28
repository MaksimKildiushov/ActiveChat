using System.Security.Claims;
using Ac.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Ac.Auth.Infrastructure;

/// <summary>
/// Регистрация эндпоинтов OpenIddict: connect/authorize, connect/token, connect/userinfo.
/// </summary>
public static class OpenIddictEndpoints
{
    /// <summary>
    /// Запрос OpenIddict доступен через Feature после обработки middleware.
    /// </summary>
    public static OpenIddictRequest? GetOpenIddictRequest(this HttpContext ctx)
    {
        var feature = ctx.Features.Get<OpenIddictServerAspNetCoreFeature>();
        var transaction = feature?.Transaction;
        return transaction?.GetType().GetProperty("Request")?.GetValue(transaction) as OpenIddictRequest;
    }

    public static IEndpointRouteBuilder MapOpenIddictConnectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMethods("connect/authorize", [HttpMethods.Get, HttpMethods.Post], AuthorizeHandler).AllowAnonymous();
        app.MapPost("connect/token", TokenHandler).AllowAnonymous();
        app.MapGet("connect/userinfo", UserinfoHandler);

        return app;
    }

    private static async Task<IResult> AuthorizeHandler(
        HttpContext ctx,
        UserManager<UserEntity> userManager,
        SignInManager<UserEntity> signInManager)
    {
        var request = ctx.GetOpenIddictRequest();
        if (request is null)
            return Results.BadRequest();

        var result = await ctx.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (result is null || !result.Succeeded || result.Principal?.Identity?.IsAuthenticated != true)
        {
            var returnUrl = ctx.Request.Path + ctx.Request.QueryString;
            return Results.Redirect($"/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var user = await userManager.GetUserAsync(result.Principal!);
        if (user is null)
            return Results.Redirect("/Account/Login");

        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType);
        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Name, user.UserName ?? user.Email ?? user.Id.ToString());
        identity.SetClaim(Claims.Email, user.Email);
        identity.SetClaim(Claims.PreferredUsername, user.UserName ?? user.Email);
        var roles = await signInManager.UserManager.GetRolesAsync(user);
        foreach (var role in roles)
            identity.AddClaim(new System.Security.Claims.Claim(OpenIddictConstants.Claims.Role, role));
        identity.SetDestinations(static c => c.Type switch
        {
            Claims.Name or Claims.PreferredUsername or Claims.Role => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        });

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources(Array.Empty<string>());

        return Results.SignIn(principal, new AuthenticationProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> TokenHandler(
        HttpContext ctx,
        UserManager<UserEntity> userManager,
        IOpenIddictApplicationManager appManager,
        IOpenIddictAuthorizationManager authManager,
        IOpenIddictTokenManager tokenManager)
    {
        var request = ctx.GetOpenIddictRequest();
        if (request is null)
            return Results.BadRequest();

        if (request.IsAuthorizationCodeGrantType())
        {
            var token = await tokenManager.FindByReferenceIdAsync(request.Code!);
            if (token is null)
                return Results.Forbid();

            var authId = await tokenManager.GetAuthorizationIdAsync(token);
            var auth = authId is not null ? await authManager.FindByIdAsync(authId) : null;
            if (auth is null)
                return Results.Forbid();

            var subject = await authManager.GetSubjectAsync(auth);
            var user = await userManager.FindByIdAsync(subject ?? "");
            if (user is null)
                return Results.Forbid();

            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType);
            identity.SetClaim(Claims.Subject, user.Id.ToString());
            identity.SetClaim(Claims.Name, user.UserName ?? user.Email);
            identity.SetClaim(Claims.Email, user.Email);
            identity.SetClaim(Claims.PreferredUsername, user.UserName ?? user.Email);
            var roles = await userManager.GetRolesAsync(user);
            foreach (var r in roles)
                identity.AddClaim(new System.Security.Claims.Claim(OpenIddictConstants.Claims.Role, r));
            identity.SetDestinations(static c => c.Type switch
            {
                Claims.Name or Claims.PreferredUsername or Claims.Role => [Destinations.AccessToken, Destinations.IdentityToken],
                _ => [Destinations.AccessToken]
            });
            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes((await authManager.GetScopesAsync(auth)).ToArray());
            principal.SetResources(Array.Empty<string>());
            return Results.SignIn(principal, new AuthenticationProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsClientCredentialsGrantType())
        {
            var application = await appManager.FindByClientIdAsync(request.ClientId!)
                ?? throw new InvalidOperationException("Application not found.");
            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType);
            identity.SetClaim(Claims.Subject, (await appManager.GetClientIdAsync(application)) ?? "");
            identity.SetClaim(Claims.Name, (await appManager.GetDisplayNameAsync(application)) ?? "");
            identity.SetDestinations(static c => c.Type is Claims.Name ? [Destinations.AccessToken] : [Destinations.AccessToken]);
            var principal = new ClaimsPrincipal(identity);
            return Results.SignIn(principal, new AuthenticationProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return Results.BadRequest(new { error = "unsupported_grant_type" });
    }

    private static async Task<IResult> UserinfoHandler(HttpContext ctx, UserManager<UserEntity> userManager)
    {
        var result = await ctx.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (result is null || !result.Succeeded)
            return Results.Challenge(authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        var user = await userManager.FindByIdAsync(result.Principal!.GetClaim(Claims.Subject)!);
        if (user is null)
            return Results.NotFound();
        return Results.Ok(new
        {
            sub = user.Id.ToString(),
            name = user.UserName ?? user.Email,
            email = user.Email,
            preferred_username = user.UserName ?? user.Email
        });
    }
}
