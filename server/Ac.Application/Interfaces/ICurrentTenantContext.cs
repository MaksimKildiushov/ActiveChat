namespace Ac.Application.Interfaces;

/// <summary>
/// Текущий тенант в рамках запроса. Устанавливается пайплайном после разрешения ChannelToken.
/// TenantDb использует SchemaName для привязки к схеме тенанта.
/// </summary>
public interface ICurrentTenantContext
{
    int TenantId { get; }
    string SchemaName { get; }

    /// <summary>Установить контекст (вызывается пайплайном после резолва токена).</summary>
    void Set(int tenantId, string schemaName);
}
