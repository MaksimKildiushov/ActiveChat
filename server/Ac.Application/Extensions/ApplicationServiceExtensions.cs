using Ac.Application.Adapters;
using Ac.Application.Contracts.Interfaces;
using Ac.Application.Handlers;
using Ac.Application.Interfaces;
using Ac.Application.Parsers;
using Ac.Application.Pipeline;
using Ac.Application.Services;
using Connections.JivoSite;
using Connections.OpenAi;
using Microsoft.Extensions.DependencyInjection;

namespace Ac.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddDi(this IServiceCollection services)
    {
        // Parsers — добавление нового канала: новый IInboundParser + IChannelDeliveryAdapter
        services.AddSingleton<IInboundParser, TelegramParser>();
        services.AddSingleton<IInboundParser, WebhookParser>();
        services.AddSingleton<IInboundParser, JivoInboundParser>();
        services.AddSingleton<InboundParserRegistry>();

        // Adapters
        services.AddSingleton<IChannelDeliveryAdapter, TelegramDeliveryAdapter>();
        services.AddSingleton<IChannelDeliveryAdapter, WebhookDeliveryAdapter>();
        services.AddSingleton<IChannelDeliveryAdapter, JivoDeliveryAdapter>();
        services.AddSingleton<IntentDispatcher>();

        // Step handlers — добавление нового шага: новый IStepHandler
        services.AddSingleton<IStepHandler, AnswerStepHandler>();
        services.AddSingleton<IStepHandler, AskClarificationStepHandler>();
        services.AddSingleton<IStepHandler, HandoffStepHandler>();
        services.AddSingleton<StepDispatcher>();

        // AI decision — OpenAi (подключение из Connections.OpenAi)
        services.AddOpenAiConnection();

        // Core services
        services.AddScoped<ClientService>();
        services.AddScoped<ConversationService>();
        services.AddScoped<InboundPipeline>();

        return services;
    }
}
