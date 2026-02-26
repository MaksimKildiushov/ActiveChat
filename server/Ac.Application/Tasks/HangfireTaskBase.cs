namespace Ac.Application.Tasks;

/// <summary>
/// Базовый класс для Hangfire-задач с единым пайплайном: валидация (загрузка и проверка данных)
/// с выбросом исключения при ошибке, затем выполнение бизнес-логики.
/// Официального шаблона у Hangfire нет — это внутренняя конвенция проекта.
/// </summary>
/// <typeparam name="TArgs">Аргументы задачи (то, что передаётся в публичный Execute).</typeparam>
/// <typeparam name="TContext">Контекст, собранный валидацией и передаваемый в ExecuteAsync.</typeparam>
public abstract class HangfireTaskBase<TArgs, TContext>
{
    /// <summary>
    /// Точка входа: сначала валидация (при ошибке — исключение и падение джобы), затем выполнение.
    /// </summary>
    protected async Task RunAsync(TArgs args, CancellationToken ct = default)
    {
        var ctx = await ValidateAsync(args, ct);
        await ExecuteAsync(ctx, ct);
    }

    /// <summary>
    /// Загружает и проверяет данные. При ошибке — логирует и выбрасывает исключение (джоба падает).
    /// При успехе возвращает контекст для <see cref="ExecuteAsync"/>.
    /// </summary>
    protected abstract Task<TContext> ValidateAsync(TArgs args, CancellationToken ct);

    /// <summary>
    /// Выполняет основную логику задачи, используя уже проверенный контекст.
    /// </summary>
    protected abstract Task ExecuteAsync(TContext ctx, CancellationToken ct);
}
