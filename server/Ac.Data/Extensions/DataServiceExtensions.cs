using Ac.Application.Interfaces;
using Ac.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ac.Data.Extensions;

public static class DataServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<ApiDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IChannelTokenResolver, ChannelTokenResolver>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IDecisionAuditRepository, DecisionAuditRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
