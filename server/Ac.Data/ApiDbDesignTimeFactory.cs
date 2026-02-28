using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using OpenIddict.EntityFrameworkCore;

namespace Ac.Data;

/// <summary>
/// Используется EF Core при выполнении <c>dotnet ef migrations add --context ApiDb</c>.
/// Создаёт ApiDb с OpenIddict, чтобы модель миграций всегда включала таблицы OpenIddict
/// и не предлагала их удалить при запуске с -StartupProject Ac.Api (где UseOpenIddict не вызывается).
/// Запускайте команду из папки Ac.Api или задайте ConnectionStrings__Default.
/// </summary>
public sealed class ApiDbDesignTimeFactory : IDesignTimeDbContextFactory<ApiDb>
{
    public ApiDb CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' not found. Run from the API project directory or set ConnectionStrings__Default.");

        var optionsBuilder = new DbContextOptionsBuilder<ApiDb>();
        optionsBuilder.UseNpgsql(connectionString);
        optionsBuilder.UseOpenIddict<Guid>();

        return new ApiDb(optionsBuilder.Options);
    }
}
