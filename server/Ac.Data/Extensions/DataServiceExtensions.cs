using Ac.Application.Interfaces;
using Ac.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Ac.Data.Extensions;

public static class DataServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IChannelTokenResolver, ChannelTokenResolver>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IDecisionAuditRepository, DecisionAuditRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
