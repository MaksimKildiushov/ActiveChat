using Ac.Application.Contracts.Interfaces;
using Ac.Application.Contracts.Models;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Ac.Application.Adapters;

public class WebhookDeliveryAdapter(ILogger<WebhookDeliveryAdapter> logger) : IChannelDeliveryAdapter
{
    public ChannelType ChannelType => ChannelType.Webhook;

    public Task DeliverAsync(OutboundMessage message, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[WebhookAdapter] -> chatId={ChatId} | intent={IntentType} | text={Text}",
            message.ChatId,
            message.Intent.GetType().Name,
            message.Intent switch
            {
                TextIntent t    => t.Text,
                HandoffIntent h => h.Message,
                _               => "(non-text)"
            });

        return Task.CompletedTask;
    }
}
