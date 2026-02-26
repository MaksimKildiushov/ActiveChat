namespace Ac.Abstractions.Options;

/// <summary>
/// Сертификат для работы https локально. Используется, например, при тестировании Telegram Бота.
/// Например, положите его сюда: "/web/wwwroot/sts_dev_cert.pfx"
/// </summary>
public class SelfHostedRunOptions
{
    public string CertificatePath { get; set; } = null!;

    public string CertificatePassword { get; set; } = null!;
}
