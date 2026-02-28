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
using Microsoft.AspNetCore.Identity;
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

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

#region Infra

// В Blazor Server HttpContext недоступен во время SignalR-вызовов.
// ScopedCurrentUser инициализируется в MainLayout из CascadingAuthenticationState.
builder.Services.AddScoped<ScopedCurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<ScopedCurrentUser>());
builder.Services.AddScoped<ICurrentUserSetter>(sp => sp.GetRequiredService<ScopedCurrentUser>());
builder.Services.AddSingleton<IDateTimeProvider, SystemClock>();
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

#endregion

builder.Services.AddIdentity<UserEntity, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApiDb>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
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

// Endpoint выхода из системы
app.MapGet("/account/logout", async (SignInManager<UserEntity> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/account/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
