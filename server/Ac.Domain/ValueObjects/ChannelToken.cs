namespace Ac.Domain.ValueObjects;

/// <summary>
/// Типизированная обёртка над Guid-токеном канала.
///
/// ЗАЧЕМ readonly struct, а не просто Guid:
///   1. Компилятор запрещает передать случайный Guid туда, где ожидается токен.
///      Невозможно перепутать channelToken с conversationId или tenantId.
///   2. Содержит бизнес-специфичные методы (ToDbString, Parse) рядом с данными.
///   3. struct — нет аллокации в куче, нет давления на GC.
///
/// КАК ИСПОЛЬЗОВАТЬ:
///   // Из маршрута (Guid уже распарсен ASP.NET Core)
///   var token = new ChannelToken(guidFromRoute);
///
///   // Из произвольной строки (принимает любой формат Guid)
///   if (ChannelToken.TryParse(rawString, out var token)) { ... }
///
///   // Запись в БД / сравнение с БД-колонкой
///   c.Token == token.ToDbString()   // "a1b2c3d4e5f6..." (32 hex, lowercase, no dashes)
/// </summary>
public readonly struct ChannelToken : IEquatable<ChannelToken>
{
    public Guid Value { get; }

    public ChannelToken(Guid value) => Value = value;

    /// <summary>Парсит любой допустимый формат Guid-строки.</summary>
    /// <exception cref="ArgumentException">Если строка не является валидным Guid.</exception>
    public static ChannelToken Parse(string raw)
        => Guid.TryParse(raw, out var guid)
            ? new ChannelToken(guid)
            : throw new ArgumentException($"'{raw}' is not a valid GUID.", nameof(raw));

    /// <summary>Тихий вариант Parse — не бросает исключение.</summary>
    public static bool TryParse(string? raw, out ChannelToken token)
    {
        if (Guid.TryParse(raw, out var guid))
        {
            token = new ChannelToken(guid);
            return true;
        }
        token = default;
        return false;
    }

    /// <summary>
    /// Формат для хранения в БД и сравнения с колонкой Channels.Token:
    /// 32 hex-символа, lowercase, без дефисов — "a1b2c3d4e5f6789012345678abcdef01".
    /// </summary>
    public string ToDbString() => Value.ToString("N");

    /// <summary>Возвращает DbString — удобно для логирования и отладки.</summary>
    public override string ToString() => Value.ToString("N");

    public bool Equals(ChannelToken other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is ChannelToken other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(ChannelToken left, ChannelToken right) => left.Equals(right);
    public static bool operator !=(ChannelToken left, ChannelToken right) => !left.Equals(right);
}
