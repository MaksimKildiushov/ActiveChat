using Ac.Data.Repositories;
using Ac.Data.Tenant;
using Microsoft.Extensions.DependencyInjection;

namespace Ac.Data.Extensions;

public static class DataServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<CurrentTenantContext>();
        services.AddScoped<ChannelTokenResolver>();
        services.AddScoped<ClientRepository>();
        services.AddScoped<ConversationRepository>();
        services.AddScoped<MessageRepository>();
        services.AddScoped<DecisionAuditRepository>();
        services.AddScoped<UnitOfWork>();

        return services;
    }
}
