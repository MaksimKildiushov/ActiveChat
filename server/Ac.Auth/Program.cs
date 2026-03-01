using System.Security.Cryptography;
using Ac.Auth.Components;
using Ac.Auth.Components.Account;
using Ac.Auth.Infrastructure;
using Ac.Data;
using Ac.Data.Accessors;
using Ac.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

#region vars

var env = builder.Environment;
// Конфиг читается из AppContext.BaseDirectory (bin/), куда MSBuild копирует linked-файлы из Ac.Api.
// ContentRootPath при dotnet run указывает на директорию проекта, а не на bin/.
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

#endregion

#region Infra
// В Blazor Server HttpContext недоступен во время SignalR-вызовов.
// ScopedCurrentUser инициализируется в MainLayout из CascadingAuthenticationState.
//builder.Services.AddScoped<ScopedCurrentUser>();
//builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<ScopedCurrentUser>());
//builder.Services.AddScoped<ICurrentUserSetter>(sp => sp.GetRequiredService<ScopedCurrentUser>());
//builder.Services.AddSingleton<IDateTimeProvider, SystemClock>();
//builder.Services.AddScoped<AuditingInterceptor>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton<IDateTimeProvider, Ac.Data.Accessors.SystemClock>();
builder.Services.AddScoped<AuditingInterceptor>();

builder.Services.AddDbContext<ApiDb>((sp, opts) =>
{
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
    opts.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
    opts.UseOpenIddict<Guid>();
});

// В Development Env ты сразу видишь детали исключения и место в коде.
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

#endregion

#region Auth
// Даёт возможность не вызывать AuthenticationStateProvider вручную в каждом компоненте.
builder.Services.AddCascadingAuthenticationState();

// Регистрирует вспомогательный сервис, который управляет перенаправлениями после операций Identity в Blazor (логин, логаут, регистрация, сброс пароля и т.д.).
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Использовать только для кеша токенов на 5 минут. В остальном - подход stateless!
builder.Services.AddDistributedMemoryCache();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddIdentityCore<UserEntity>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;        
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApiDb>()
    .AddSignInManager()
    .AddDefaultTokenProviders();


//????
//builder.Services.ConfigureApplicationCookie(options =>
//{
//    options.LoginPath = "/account/login";
//    options.AccessDeniedPath = "/account/login";
//    options.ExpireTimeSpan = TimeSpan.FromHours(8);
//    options.SlidingExpiration = true;
//});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApiDb>()
            .ReplaceDefaultEntities<Guid>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
            .SetTokenEndpointUris("connect/token");

        options.AllowAuthorizationCodeFlow()
            .AllowClientCredentialsFlow()
            .RequireProofKeyForCodeExchange();

        // Шифрование: строка из конфига (OidcServer:EncryptionKey), 32 байта — не файл на диске.
        options.AddEncryptionKey(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["OidcServer:EncryptionKey"])));
        // Подпись: временный dev-сертификат (генерируется/берётся из хранилища, не файл на диске). В проде — AddSigningCertificate/AddSigningKey.
        options.AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthorization(options =>
{
    // Проверка по JWT: в токене роль добавляется как ClaimTypes.Role.
    options.AddPolicy("BearerAdmin", policy =>
    {
        policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireRole("Admin");
    });
});

// Регистрация OIDC-клиента Ac.Admin и scope'ов при старте
builder.Services.AddHostedService<OpenIddictClientSeeder>();

#endregion


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapOpenIddictConnectEndpoints();

// Выход из сессии Auth (для «войти под другой учётной записью» в Admin). Редирект только на Admin base.
app.MapGet("connect/logout", async (
    HttpContext ctx,
    SignInManager<UserEntity> signInManager,
    IConfiguration config,
    string? post_logout_redirect_uri) =>
{
    await signInManager.SignOutAsync();
    var adminRedirectUri = config["OidcServer:AdminRedirectUri"] ?? "";
    var allowedBase = adminRedirectUri.Replace("/signin-oidc", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
    if (string.IsNullOrEmpty(post_logout_redirect_uri) || string.IsNullOrEmpty(allowedBase))
        return Results.Redirect("/");
    if (!post_logout_redirect_uri.StartsWith(allowedBase, StringComparison.OrdinalIgnoreCase))
        return Results.Redirect("/");
    return Results.Redirect(post_logout_redirect_uri);
}).AllowAnonymous();

// API для Admin: создание пользователя (Bearer token + роль Admin)
app.MapPost("api/invitations/create-user", async (
    UserManager<UserEntity> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    CreateUserRequest body) =>
{
    var email = body.Email?.Trim();
    if (string.IsNullOrEmpty(email))
        return Results.BadRequest("Email required.");

    var existing = await userManager.FindByEmailAsync(email);
    if (existing is not null)
        return Results.Ok(new { userId = existing.Id });

    if (string.IsNullOrEmpty(body.Password) || body.Password.Length < 6)
        return Results.BadRequest("Password required (min 6) for new user.");

    var user = new UserEntity
    {
        UserName = email,
        Email = email,
        EmailConfirmed = false,
        DisplayName = body.DisplayName?.Trim() ?? email,
        AuthorId = Guid.Empty,
        Created = DateTime.UtcNow,
    };
    var result = await userManager.CreateAsync(user, body.Password);
    if (!result.Succeeded)
        return Results.BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));

    var role = body.Role?.Trim() ?? "TenantUser";
    if (!string.IsNullOrEmpty(role))
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
    }
    return Results.Ok(new { userId = user.Id });
}).RequireAuthorization("BearerAdmin");

// --- Страница создания JWT токенов (Authorization Code + PKCE) ---
const string TokensClientId = "Ac.Auth.Tokens";
const string TokensCookieName = "TokensCodeVerifier";

app.MapPost("tokens/authorize", async (
    HttpContext ctx,
    IConfiguration config) =>
{
    var authority = (config["Infra:AuthBaseUrl"] ?? config["OidcServer:Authority"])?.TrimEnd('/')
        ?? $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    var redirectUri = authority + "/tokens/callback";
    var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    var codeVerifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    var challengeBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(codeVerifier));
    var codeChallenge = Convert.ToBase64String(challengeBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    ctx.Response.Cookies.Append(TokensCookieName, codeVerifier, new CookieOptions
    {
        Path = "/tokens",
        MaxAge = TimeSpan.FromMinutes(5),
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Secure = !ctx.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
    });
    var authorizeUrl = authority + "/connect/authorize?" + new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["client_id"] = TokensClientId,
        ["redirect_uri"] = redirectUri,
        ["response_type"] = "code",
        ["scope"] = "openid profile",
        ["state"] = state,
        ["code_challenge"] = codeChallenge,
        ["code_challenge_method"] = "S256"
    }.Select(p => new KeyValuePair<string, string>(p.Key, p.Value))).ReadAsStringAsync().Result;
    return Results.Redirect(authorizeUrl);
}).RequireAuthorization();

app.MapGet("tokens/callback", async (
    HttpContext ctx,
    IConfiguration config,
    string? code,
    string? state,
    string? error) =>
{
    if (!string.IsNullOrEmpty(error))
    {
        return Results.Content(
            $"""
            <!DOCTYPE html><html><head><meta charset="utf-8"><title>Ошибка</title></head><body>
            <p>Ошибка: {System.Net.WebUtility.HtmlEncode(error)}</p>
            <a href="/tokens">Вернуться к созданию токена</a>
            </body></html>
            """,
            "text/html; charset=utf-8");
    }
    if (string.IsNullOrEmpty(code) || !ctx.Request.Cookies.TryGetValue(TokensCookieName, out var codeVerifier))
    {
        return Results.Redirect("/tokens");
    }
    ctx.Response.Cookies.Delete(TokensCookieName, new CookieOptions { Path = "/tokens" });
    var authority = (config["Infra:AuthBaseUrl"] ?? config["OidcServer:Authority"])?.TrimEnd('/')
        ?? $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    var redirectUri = authority + "/tokens/callback";
    var clientSecret = config["OidcServer:TokensClientSecret"] ?? "Ac.Auth.Tokens-secret-change-in-production";
    var tokenUrl = authority + "/connect/token";
    using var http = new HttpClient();
    var form = new Dictionary<string, string>
    {
        ["grant_type"] = "authorization_code",
        ["code"] = code,
        ["redirect_uri"] = redirectUri,
        ["client_id"] = TokensClientId,
        ["client_secret"] = clientSecret,
        ["code_verifier"] = codeVerifier!
    };
    var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
    {
        Content = new FormUrlEncodedContent(form)
    };
    var response = await http.SendAsync(req);
    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        return Results.Content(
            $"""
            <!DOCTYPE html><html><head><meta charset="utf-8"><title>Ошибка обмена кода</title></head><body>
            <p>Ошибка {response.StatusCode}: {System.Net.WebUtility.HtmlEncode(body)}</p>
            <a href="/tokens">Вернуться к созданию токена</a>
            </body></html>
            """,
            "text/html; charset=utf-8");
    }
    using var doc = System.Text.Json.JsonDocument.Parse(body);
    var accessToken = doc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : "";
    var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var ex) ? ex.GetInt32() : 0;
    var tokenHtml = System.Net.WebUtility.HtmlEncode(accessToken ?? "");
    var expiresText = expiresIn > 0 ? $"Срок действия: {expiresIn} сек." : "";
    return Results.Content(
        $"""
        <!DOCTYPE html><html><head><meta charset="utf-8"><title>JWT токен создан</title></head><body>
        <h2>JWT токен создан</h2>
        <p>{expiresText}</p>
        <textarea id="t" readonly rows="6" style="width:100%;">{tokenHtml}</textarea>
        <br><button onclick="navigator.clipboard.writeText(document.getElementById('t').value)">Скопировать</button>
        <a href="/tokens">Создать ещё</a>
        </body></html>
        """,
        "text/html; charset=utf-8");
}).AllowAnonymous();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

record CreateUserRequest(string? Email, string? Password, string? DisplayName, string? Role);
