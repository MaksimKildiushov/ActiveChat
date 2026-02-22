using Ac.Api.Filters;
using Ac.Application.Extensions;
using Ac.Data;
using Ac.Data.Accessors;
using Ac.Data.Extensions;
using Ac.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
builder.Configuration
    .SetBasePath(env.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

ConfigureApp(app, app.Environment);

app.Run();


void ConfigureServices(IServiceCollection services, IConfiguration cfg)
{

    #region Infra

    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUser, HttpCurrentUser>();
    services.AddSingleton<IDateTimeProvider, SystemClock>();
    services.AddScoped<AuditingInterceptor>();

    // Использовать только для кеша токенов на 5 минут. В остальном - подход stateless!
    services.AddDistributedMemoryCache();

    services.AddDbContext<ApiDb>((sp, opts) =>
    {
        opts.UseNpgsql(cfg.GetConnectionString("Default"));
        opts.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
    });

    services.AddControllers();
    services.AddOpenApi();
    services.AddScoped<ChannelTokenAuthFilter>();

    services.AddApplication();
    services.AddInfrastructure();

    #endregion

    #region Auth

    services.AddIdentityCore<UserEntity>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApiDb>()
    .AddDefaultTokenProviders();

    var jwtSettings = cfg.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]
        ?? throw new InvalidOperationException("Jwt:Key is not configured."));

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero,
            };
        });

    services.AddAuthorization();

    #endregion

}

static void ConfigureApp(WebApplication app, IWebHostEnvironment env)
{
    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
}
