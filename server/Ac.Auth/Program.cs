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
    // Проверка по JWT: схема OpenIddict Validation + claim "role" = "Admin" (в токене от нашего сервера роль идёт как claim "role").
    options.AddPolicy("BearerAdmin", policy =>
    {
        policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireAssertion(ctx => ctx.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Admin")
            || ctx.User.HasClaim("role", "Admin"));
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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

record CreateUserRequest(string? Email, string? Password, string? DisplayName, string? Role);
