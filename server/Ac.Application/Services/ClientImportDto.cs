namespace Ac.Application.Services;

/// <summary>Одна строка импорта клиентов (из Excel или иного источника).</summary>
public sealed class ClientImportRow
{
    /// <summary>Индекс строки (1-based) для отчёта об ошибках.</summary>
    public int RowIndex { get; init; }

    public string? ChannelUserId { get; init; }
    public string? OverrideUserId { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    /// <summary>Телефон в исходном виде — сервис нормализует перед сохранением.</summary>
    public string? Phone { get; init; }
    public string? TraceId { get; init; }
    public string? Timezone { get; init; }
    public bool IsBlocked { get; init; }
    /// <summary>Дополнительные поля (ключ — имя поля). Будут отсортированы по ключу при сохранении.</summary>
    public IReadOnlyDictionary<string, string> AdditionalFields { get; init; } = new Dictionary<string, string>();
}

/// <summary>Результат импорта одной строки.</summary>
public sealed class ClientImportRowResult
{
    public int RowIndex { get; init; }
    public string? Error { get; init; }
}

/// <summary>Результат импорта клиентов.</summary>
public sealed class ClientImportResult
{
    public IReadOnlyList<ClientImportRowResult> RowResults { get; init; } = [];
}
