using Ac.Application.Interfaces;
using Ac.Application.Models;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Ac.Application.Adapters;

public class WebhookAdapter(ILogger<WebhookAdapter> logger) : IChannelDeliveryAdapter
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
