using Ac.Api.Filters;
using Ac.Application.Pipeline;
using Ac.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Ac.Api.Controllers;

[ApiController]
[Route("channel")]
[ServiceFilter(typeof(ChannelTokenAuthFilter))]
public class ChannelController(InboundPipeline pipeline, ILogger<ChannelController> logger) : ControllerBase
{
    /// <summary>
    /// Принимает входящее сообщение от любого канала.
    /// <paramref name="channelToken"/> — GUID канала. ASP.NET Core парсит его автоматически;
    /// невалидный формат возвращает 400 ещё до входа в метод.
    /// Body — произвольный JSON (парсинг делает соответствующий IInboundParser).
    /// </summary>
    /// <example>POST /channel/a1b2c3d4-e5f6-7890-1234-5678abcdef01</example>
    [HttpPost("{channelToken:guid}")]
    [Consumes("application/json")]
    public async Task<IActionResult> PostAsync(
        [FromRoute] Guid channelToken,
        CancellationToken ct)
    {
        var token = new ChannelToken(channelToken);

        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(rawBody))
            return BadRequest(new { error = "Request body must not be empty." });

        try
        {
            await pipeline.ProcessAsync(token, rawBody, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Pipeline rejected request for token={Token}", token);
            return BadRequest(new { error = ex.Message });
        }
    }
}
