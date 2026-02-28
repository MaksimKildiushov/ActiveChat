using Ac.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ac.Auth.DesignTime;

/// <summary>
/// Для dotnet ef migrations (design-time). Создаёт ApiDb с OpenIddict.
/// </summary>
public sealed class ApiDbFactory : IDesignTimeDbContextFactory<ApiDb>
{
    public ApiDb CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var builder = new DbContextOptionsBuilder<ApiDb>();
        builder.UseNpgsql(config.GetConnectionString("Default"));
        builder.UseOpenIddict<Guid>();

        return new ApiDb(builder.Options);
    }
}
