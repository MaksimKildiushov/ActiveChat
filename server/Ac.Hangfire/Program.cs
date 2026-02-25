using Ac.Application.Extensions;
using Ac.Data;
using Ac.Data.Accessors;
using Ac.Data.Extensions;
using Ac.Hangfire.BackgroundServices;
using Ac.Hangfire.Mock;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder();

var env = builder.Environment;
// Конфиг читается из AppContext.BaseDirectory (bin/), куда MSBuild копирует linked-файлы из Ac.Api.
// ContentRootPath при dotnet run указывает на директорию проекта, а не на bin/.
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

#region Infra

builder.Services.AddScoped<ICurrentUser, HttpCurrentUserMock>();
builder.Services.AddSingleton<IDateTimeProvider, SystemClock>();
builder.Services.AddScoped<AuditingInterceptor>();

var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<ApiDb>((sp, opts) =>
{
    opts.UseNpgsql(connectionString);
    opts.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
});

builder.Services.AddDbContext<TenantDb>((sp, opts) =>
{
    opts.UseNpgsql(connectionString);
    opts.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
});

builder.Services.AddInfrastructure();

#endregion

#region Hangfire

// Add Hangfire services.
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString))
    );//.UseSerilogLogProvider()

// Add the processing server as IHostedService
builder.Services.AddHangfireServer();

GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3 });

#endregion

builder.Services.AddDi();

// Фоновые сервисы: NOTIFY-слушатель и пуллинг как бекап
builder.Services.AddHostedService<EventListenerService>();
builder.Services.AddHostedService<EventPollingService>();

var app = builder.Build();

app.MapGet("/", () => "Ac.Hangfire — Event listener and polling are running.");

app.Run();
