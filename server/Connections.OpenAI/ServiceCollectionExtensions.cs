using Ac.Application.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Connections.OpenAi;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует сервис принятия решений на основе OpenAi и конфигурацию (секция OpenAi, при необходимости OpenAi:Clients из appsettings).
    /// </summary>
    public static IServiceCollection AddOpenAiConnection(this IServiceCollection services)
    {
        services.AddHttpClient("OpenAi");
        services.AddOptions<OpenAiOptions>()
            .BindConfiguration(OpenAiOptions.SectionName);
        services.AddScoped<IAiDecisionService, OpenAiDecisionService>();
        return services;
    }
}
