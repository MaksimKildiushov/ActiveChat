namespace Ac.Domain.ValueObjects;

/// <summary>
/// Типизированная обёртка над строковым токеном канала.
///
/// ЗАЧЕМ readonly struct, а не просто string:
///   1. Компилятор запрещает передать случайную строку туда, где ожидается токен.
///      Невозможно перепутать channelToken с tenantId или любой другой строкой.
///   2. Нормализация (lowercase, без дефисов) происходит ОДИН раз при создании,
///      а не размазана по всему коду.
///   3. struct — нет аллокации в куче, нет давления на GC.
///      Это важно, так как токен участвует в каждом входящем запросе.
///
/// КАК ИСПОЛЬЗОВАТЬ:
///   // Парсинг из HTTP-заголовка (выбрасывает ArgumentException при невалидном токене)
///   var token = ChannelToken.Parse(httpHeader);
///
///   // Тихий вариант — когда нельзя бросать исключение
///   if (ChannelToken.TryParse(rawValue, out var token))
///       await resolver.ResolveAsync(token);
///
///   // Передача в репозиторий — только строка нужна внутри EF-запроса
///   var channel = await db.Channels.FirstOrDefaultAsync(c => c.Token == token.Value);
///
/// КОГДА ДОБАВЛЯТЬ В PIPELINE:
///   Сейчас IngressController передаёт string напрямую в InboundPipeline.
///   Чтобы активировать ChannelToken — поменять сигнатуру ProcessAsync(ChannelToken token, ...)
///   и вызывать ChannelToken.Parse() в контроллере до вызова pipeline.
///   Тогда невалидный токен отвергается ещё на уровне HTTP, не доходя до БД.
/// </summary>
public readonly struct ChannelToken : IEquatable<ChannelToken>
{
    /// <summary>Нормализованное значение: lowercase, без дефисов, без пробелов.</summary>
    public string Value { get; }

    private ChannelToken(string value) => Value = value;

    /// <summary>
    /// Создаёт ChannelToken из сырой строки.
    /// Нормализует: приводит к lowercase, удаляет дефисы и пробелы.
    /// </summary>
    /// <exception cref="ArgumentException">Если строка пустая или короче 8 символов после нормализации.</exception>
    public static ChannelToken Parse(string raw)
    {
        var normalized = Normalize(raw);

        if (normalized.Length < 8)
            throw new ArgumentException($"Channel token is too short after normalization: '{normalized}'.", nameof(raw));

        return new ChannelToken(normalized);
    }

    /// <summary>Тихий вариант Parse — не бросает исключение.</summary>
    public static bool TryParse(string? raw, out ChannelToken token)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            token = default;
            return false;
        }

        var normalized = Normalize(raw);
        if (normalized.Length < 8)
        {
            token = default;
            return false;
        }

        token = new ChannelToken(normalized);
        return true;
    }

    private static string Normalize(string raw)
        => raw.Replace("-", "").Replace(" ", "").ToLowerInvariant();

    public bool Equals(ChannelToken other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is ChannelToken other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static bool operator ==(ChannelToken left, ChannelToken right) => left.Equals(right);
    public static bool operator !=(ChannelToken left, ChannelToken right) => !left.Equals(right);
}
