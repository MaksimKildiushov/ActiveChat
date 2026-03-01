using OpenIddict.Abstractions;

namespace Ac.Hangfire.Jobs;

/// <summary>
/// Еженедельная очистка устаревших токенов и авторизаций OpenIddict (старше месяца).
/// </summary>
public class OpenIddictTokenPruneJob(
    IOpenIddictTokenManager tokenManager,
    IOpenIddictAuthorizationManager authorizationManager,
    ILogger<OpenIddictTokenPruneJob> logger)
{
    /// <summary>
    /// Удаляет токены и авторизации, созданные/истёкшие раньше чем порог (по умолчанию — месяц назад).
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var threshold = DateTimeOffset.UtcNow.AddMonths(-1);
        logger.LogInformation("OpenIddict prune started, threshold: {Threshold:O}", threshold);

        await tokenManager.PruneAsync(threshold, cancellationToken);
        await authorizationManager.PruneAsync(threshold, cancellationToken);

        logger.LogInformation("OpenIddict prune completed.");
    }
}
