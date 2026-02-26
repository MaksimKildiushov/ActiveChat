using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Ac.Application.Tasks;

/// <summary>
/// Задача для дублирования новых сообщений в TG чате-админке (посредник n8n).
/// </summary>
public class CloneChatMessageToTgTask(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<CloneChatMessageToTgTask> logger)
{
    /// <summary>
    /// Отправляет уведомление о новом сообщении через n8n webhook (для UserMessage: tenantId, conversationId, messageId, userId).
    /// </summary>
    public async Task Execute(int tenantId, int chatId, int messageId, int userId)
    {
        await ExecuteCore(chatId, messageId, userId: userId);
    }

    /// <summary>
    /// Отправляет уведомление оператору о новом сообщении через n8n webhook (для OperatorMessage).
    /// </summary>
    public async Task Execute(int chatId, int messageId, Guid operatorId)
    {
        await ExecuteCore(chatId, messageId, operatorId: operatorId);
    }

    private async Task ExecuteCore(int chatId, int messageId, int? userId = null, Guid? operatorId = null)
    {
        logger.LogDebug(
            "TaskCloneChatMessageToTg: ChatId={ChatId}, MessageId={MessageId}, UserId={UserId}, OperatorId={OperatorId}",
            chatId, messageId, userId, operatorId);

        var webhookUrl = configuration["n8n:UserMessageWebhookUrl"];
        if (string.IsNullOrEmpty(webhookUrl))
        {
            logger.LogError("n8n:UserMessageWebhookUrl not configured in appsettings");
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            var requestBody = userId.HasValue
                ? (object)new { chatId, messageId, userId = userId.Value }
                : new { chatId, messageId, operatorId };

            var response = await client.PostAsJsonAsync(webhookUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError(
                    "Failed to call n8n webhook. Status: {Status}, Response: {Response}",
                    response.StatusCode, errorContent);
                return;
            }

            logger.LogDebug(
                "Successfully called n8n webhook: ChatId={ChatId}, MessageId={MessageId}",
                chatId, messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while calling n8n webhook");
        }
    }
}
