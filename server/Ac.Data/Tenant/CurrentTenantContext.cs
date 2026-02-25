namespace Ac.Data.Tenant;

/// <summary>
/// Scoped: один экземпляр на запрос. Пайплайн устанавливает TenantId и SchemaName после резолва ChannelToken.
/// </summary>
public sealed class CurrentTenantContext
{
    public int TenantId { get; private set; }
    public string SchemaName { get; private set; } = null!;

    public void Set(int tenantId, string schemaName)
    {
        TenantId = tenantId;
        SchemaName = schemaName;
    }
}
