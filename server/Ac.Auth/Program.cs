using Ac.Auth.Components;
using Ac.Auth.Components.Account;
using Ac.Data;
using Ac.Data.Accessors;
using Ac.Domain.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddSingleton<IDateTimeProvider, SystemClock>();
builder.Services.AddScoped<AuditingInterceptor>();

builder.Services.AddDbContext<ApiDb>((sp, opts) =>
{
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
    opts.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
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

        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
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

//builder.Services.AddAuthorization();
#endregion

#region DI

//builder.Services.AddSingleton<IEmailSender<UserEntity>, IdentityNoOpEmailSender>();

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

//???
//app.UseAuthentication();
//app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
