using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Ac.Application.Tasks;

/// <summary>
/// Задача для дублирования новых сообщений в TG чате-админке (посредник n8n).
/// </summary>
public class TaskCloneChatMessageToTg(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TaskCloneChatMessageToTg> logger)
{
    /// <summary>
    /// Отправляет уведомление пользователю о новом сообщении через n8n webhook
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="messageId">ID сообщения</param>
    /// <param name="userId">ID пользователя</param>
    public async Task Execute(int chatId, int messageId, Guid userId)
    {
        logger.LogDebug(
            "TaskPushUserMessage: ChatId={ChatId}, MessageId={MessageId}, UserId={UserId}",
            chatId, messageId, userId);

        var webhookUrl = configuration["n8n:UserMessageWebhookUrl"];
        if (string.IsNullOrEmpty(webhookUrl))
        {
            logger.LogError("n8n:UserMessageWebhookUrl not configured in appsettings");
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            
            var requestBody = new
            {
                chatId,
                messageId,
                userId
            };

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
                "Successfully called n8n webhook: ChatId={ChatId}, MessageId={MessageId}, UserId={UserId}",
                chatId, messageId, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while calling n8n webhook");
        }
    }
}
