using Ac.Admin.Components;
using Ac.Admin.Infrastructure;
using Ac.Application.Services;
using Ac.Application.Extensions;
using Ac.Data;
using Ac.Data.Accessors;
using Ac.Data.Extensions;
using Ac.Domain.Entities;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;

// Запуск миграций тенантов вместо веб-приложения (для CI/CD: dotnet run -- migrate_tenants)
var cmdArgs = Environment.GetCommandLineArgs();
if (cmdArgs.Contains("migrate_tenants", StringComparer.OrdinalIgnoreCase))
{
    var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    if (envName is null)
    {
        Console.Error.WriteLine("Choose Environment (set ASPNETCORE_ENVIRONMENT var)!");
        Environment.Exit(1);
    }

    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{envName}.json", optional: false)
        .AddEnvironmentVariables()
        .Build();

    var connectionString = config.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("Connection string 'Default' not found. Set ConnectionStrings__Default or ensure appsettings.json is present.");
        Environment.Exit(1);
    }

    var results = await TenantMigrationRunner.RunAsync(connectionString);
    var applied = results.Count(r => r.Success && r.AppliedCount > 0);
    var failed = results.Count(r => !r.Success);

    foreach (var r in results)
    {
        var status = r.Success ? (r.AppliedCount > 0 ? "[OK]" : "[--]") : "[FAIL]";
        Console.WriteLine($"  {status} {r.Schema}: {r.Message}");
    }

    Console.WriteLine();
    Console.WriteLine($"Done. Applied: {applied}, Up to date: {results.Count - applied - failed}, Failed: {failed}");
    Environment.Exit(failed > 0 ? 1 : 0);
}

var builder = WebApplication.CreateBuilder(cmdArgs);

var env = builder.Environment;
// Конфиг читается из AppContext.BaseDirectory (bin/), куда MSBuild копирует linked-файлы из Ac.Api.
// ContentRootPath при dotnet run указывает на директорию проекта, а не на bin/.
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

#region Infra

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddHttpContextAccessor();

// В Blazor Server HttpContext недоступен во время SignalR-вызовов.
// ScopedCurrentUser инициализируется в MainLayout из CascadingAuthenticationState.
builder.Services.AddScoped<ScopedCurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<ScopedCurrentUser>());
builder.Services.AddScoped<ICurrentUserSetter>(sp => sp.GetRequiredService<ScopedCurrentUser>());
builder.Services.AddSingleton<IDateTimeProvider, Ac.Data.Accessors.SystemClock>();
builder.Services.AddScoped<AuditingInterceptor>();

// Использовать только для кеша токенов на 5 минут. В остальном - подход stateless!
builder.Services.AddDistributedMemoryCache();

builder.Services.AddDbContext<ApiDb>((sp, opts) =>
{
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
    opts.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
});

builder.Services.AddDbContext<TenantDb>((sp, opts) =>
{
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
    opts.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
});

builder.Services.AddDi();
builder.Services.AddInfrastructure();

builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>();

#endregion

// OIDC: вход через Ac.Auth
var oidcAuthority = (builder.Configuration["Infra:AuthBaseUrl"] ?? builder.Configuration["AuthForAdmin:Authority"])?.TrimEnd('/') ?? "https://localhost:7189";
var oidcClientId = builder.Configuration["AuthForAdmin:ClientId"] ?? "Ac.Admin";
var oidcClientSecret = builder.Configuration["AuthForAdmin:ClientSecret"] ?? "Ac.Admin-secret-change-in-production";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/challenge-oidc";
    options.AccessDeniedPath = "/account/login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = oidcAuthority;
    options.ClientId = oidcClientId;
    options.ClientSecret = oidcClientSecret;
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";
    options.RemoteSignOutPath = "/signout-oidc";
    options.TokenValidationParameters.NameClaimType = "preferred_username";
    options.TokenValidationParameters.RoleClaimType = "role";
});

builder.Services.AddAuthorization();

// Hangfire: дашборд в Admin, хранилище — та же PostgreSQL (таблицы в схеме hangfire)
var hangfireConnectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrEmpty(hangfireConnectionString))
{
    builder.Services.AddHangfire(config =>
        config.UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(hangfireConnectionString)));
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthorizationFilter()],
});

app.UseAntiforgery();

// OIDC: редирект на Ac.Auth для входа
app.MapGet("/challenge-oidc", (HttpContext ctx, string? returnUrl) =>
{
    var redirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
    return Results.Challenge(
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = redirectUri },
        [OpenIdConnectDefaults.AuthenticationScheme]);
}).AllowAnonymous();

// Выход: локальный sign-out (cookie) + при необходимости редирект на Auth
app.MapGet("/account/logout", (HttpContext ctx) =>
{
    return Results.SignOut(authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme],
        properties: new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/account/login" });
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
