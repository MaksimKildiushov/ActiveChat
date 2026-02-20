using Ac.Application.Pipeline;
using Microsoft.AspNetCore.Mvc;

namespace Ac.Api.Controllers;

[ApiController]
[Route("ingress")]
public class IngressController(InboundPipeline pipeline, ILogger<IngressController> logger) : ControllerBase
{
    /// <summary>
    /// Принимает входящее сообщение от любого канала.
    /// X-Channel-Token идентифицирует канал и тенанта.
    /// Body — произвольный JSON (парсинг делает соответствующий IInboundParser).
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> PostAsync(
        [FromHeader(Name = "X-Channel-Token")] string? channelToken,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(channelToken))
            return BadRequest(new { error = "Header X-Channel-Token is required." });

        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(rawBody))
            return BadRequest(new { error = "Request body must not be empty." });

        try
        {
            await pipeline.ProcessAsync(channelToken, rawBody, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Pipeline rejected request for token={Token}", channelToken);
            return BadRequest(new { error = ex.Message });
        }
    }
}
