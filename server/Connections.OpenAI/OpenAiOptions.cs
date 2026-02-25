namespace Connections.OpenAi;

/// <summary>
/// Настройки подключения к OpenAi API.
/// Ключ и модель можно задать глобально (ApiKey, Model) или по тенанту в OpenAi:Clients:{SchemaName}.
/// </summary>
public class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    /// <summary>API-ключ по умолчанию. Используется, если для схемы нет записи в Clients.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Модель чата по умолчанию (например, gpt-4o-mini, gpt-4o).</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>Настройки по тенантам (ключ — схема, например t_9729280803). Подгружаются из OpenAi:Clients в appsettings.</summary>
    public Dictionary<string, OpenAiClientOptions>? Clients { get; set; }
}

/// <summary>
/// Настройки клиента OpenAi для одной схемы (тенанта).
/// </summary>
public class OpenAiClientOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
}
