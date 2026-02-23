using Ac.Application.Adapters;
using Ac.Application.Contracts.Interfaces;
using Ac.Application.Handlers;
using Ac.Application.Interfaces;
using Ac.Application.Parsers;
using Ac.Application.Pipeline;
using Ac.Application.Services;
using Connections.JivoSite;
using Microsoft.Extensions.DependencyInjection;

namespace Ac.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddDi(this IServiceCollection services)
    {
        // Parsers — добавление нового канала: новый IInboundParser + IChannelDeliveryAdapter
        services.AddSingleton<IInboundParser, TelegramLikeParser>();
        services.AddSingleton<IInboundParser, WebhookParser>();
        services.AddSingleton<IInboundParser, JivoInboundParser>();
        services.AddSingleton<InboundParserRegistry>();

        // Adapters
        services.AddSingleton<IChannelDeliveryAdapter, TelegramAdapter>();
        services.AddSingleton<IChannelDeliveryAdapter, WebhookAdapter>();
        services.AddSingleton<IntentDispatcher>();

        // Step handlers — добавление нового шага: новый IStepHandler
        services.AddSingleton<IStepHandler, AnswerStepHandler>();
        services.AddSingleton<IStepHandler, AskClarificationStepHandler>();
        services.AddSingleton<IStepHandler, HandoffStepHandler>();
        services.AddSingleton<StepDispatcher>();

        // AI decision (заглушка — заменить на реальную реализацию)
        services.AddScoped<IAiDecisionService, StubAiDecisionService>();

        // Core services
        services.AddScoped<ConversationService>();
        services.AddScoped<InboundPipeline>();

        return services;
    }
}
