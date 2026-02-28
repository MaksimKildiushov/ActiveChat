using System.Text.RegularExpressions;
using System.Text.Json;
using Ac.Data;
using Ac.Data.Repositories;
using Ac.Domain.Entities;
using Ac.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ac.Application.Services;

public class ClientService(ClientRepository clients, TenantDb tenantDb)
{
    private static readonly Regex SchemaNameRegex = new(@"^t_[a-zA-Z0-9_]+$", RegexOptions.Compiled);
    private readonly TenantDb _tenantDb = tenantDb;

    /// <summary>
    /// Находит клиента по приоритету:
    /// 1) OverrideUserId
    /// 2) Email + Phone (если телефон не задан — только Email)
    /// 3) ChannelUserId
    /// Если не найден — создаёт клиента и заполняет все переданные поля.
    /// </summary>
    public async Task<ClientEntity> GetOrCreateAsync(
        ChannelContext channelCtx,
        string? channelUserId,
        string? overrideUserId,
        string? email,
        string? phone,
        string? displayName = null,
        string? traceId = null,
        string? timezone = null,
        string? metadataJson = null,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(overrideUserId))
        {
            var byOverride = await clients.FindByOverrideUserIdAsync(overrideUserId, ct);
            if (byOverride is not null)
                return byOverride;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var byEmailAndPhone = await clients.FindByEmailAndPhoneAsync(email, phone, ct);
            if (byEmailAndPhone is not null)
                return byEmailAndPhone;
        }

        if (!string.IsNullOrWhiteSpace(channelUserId))
        {
            var byChannel = await clients.FindByChannelUserIdAsync(channelUserId, ct);
            if (byChannel is not null)
                return byChannel;
        }

        var client = new ClientEntity
        {
            ChannelUserId = channelUserId,
            OverrideUserId = overrideUserId,
            DisplayName = displayName,
            Email = email,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : NormalizePhone(phone),
            TraceId = traceId,
            Timezone = timezone,
            MetadataJson = metadataJson,
        };

        return await clients.CreateAsync(client, ct);
    }

    /// <summary>
    /// Импорт клиентов. Предполагается, что CurrentTenantContext уже настроен на нужную схему.
    /// Телефоны нормализуются; доп. поля сохраняются в алфавитном порядке по ключу.
    /// </summary>
    public async Task<ClientImportResult> ImportClientsAsync(
        IReadOnlyList<ClientImportRow> rows,
        CancellationToken ct = default)
    {
        var results = new List<ClientImportRowResult>(rows.Count);

        foreach (var row in rows)
        {
            try
            {
                var normalizedPhone = NormalizePhone(row.Phone);

                // 1) Пытаемся найти существующего клиента по той же логике, что и GetOrCreateAsync.
                ClientEntity? existing = null;

                // 1. OverrideUserId
                if (!string.IsNullOrWhiteSpace(row.OverrideUserId))
                {
                    existing = await _tenantDb.Clients
                        .FirstOrDefaultAsync(c => c.OverrideUserId == row.OverrideUserId, ct);
                }

                // 2. Email + Phone (игнорируя, что phone может быть null)
                if (existing is null && !string.IsNullOrWhiteSpace(row.Email))
                {
                    if (!string.IsNullOrWhiteSpace(normalizedPhone))
                    {
                        existing = await _tenantDb.Clients
                            .FirstOrDefaultAsync(c =>
                                c.Email != null && c.Email == row.Email &&
                                c.Phone != null && c.Phone == normalizedPhone, ct);
                    }

                    if (existing is null)
                    {
                        existing = await _tenantDb.Clients
                            .FirstOrDefaultAsync(c => c.Email != null && c.Email == row.Email, ct);
                    }
                }

                // 3. ChannelUserId
                if (existing is null && !string.IsNullOrWhiteSpace(row.ChannelUserId))
                {
                    existing = await _tenantDb.Clients
                        .FirstOrDefaultAsync(c => c.ChannelUserId == row.ChannelUserId, ct);
                }

                // Доп. поля: сохраняем существующие и перезаписываем только то, что пришло в импорте.
                var baseAdditional = existing?.AdditionalFields
                    ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var mergedAdditional = new Dictionary<string, string>(baseAdditional, StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in row.AdditionalFields)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                        mergedAdditional[kvp.Key] = kvp.Value;
                }

                if (existing is null)
                {
                    // Вставка нового клиента через сущность, AuditingInterceptor выставит Created/AuthorId.
                    var client = new ClientEntity
                    {
                        ChannelUserId = row.ChannelUserId,
                        OverrideUserId = row.OverrideUserId,
                        DisplayName = row.DisplayName,
                        Email = row.Email,
                        Phone = normalizedPhone,
                        TraceId = row.TraceId,
                        Timezone = row.Timezone,
                        IsBlocked = row.IsBlocked
                    };

                    if (mergedAdditional.Count > 0)
                    {
                        var ordered = new SortedDictionary<string, string>(mergedAdditional, StringComparer.OrdinalIgnoreCase);
                        client.AdditionalFields = ordered;
                    }

                    await _tenantDb.Clients.AddAsync(client, ct);
                }
                else
                {
                    // Обновление существующего клиента
                    if (!string.IsNullOrWhiteSpace(row.ChannelUserId))
                        existing.ChannelUserId = row.ChannelUserId;
                    if (!string.IsNullOrWhiteSpace(row.OverrideUserId))
                        existing.OverrideUserId = row.OverrideUserId;
                    if (!string.IsNullOrWhiteSpace(row.DisplayName))
                        existing.DisplayName = row.DisplayName;
                    if (!string.IsNullOrWhiteSpace(row.Email))
                        existing.Email = row.Email;
                    if (!string.IsNullOrWhiteSpace(normalizedPhone))
                        existing.Phone = normalizedPhone;
                    if (!string.IsNullOrWhiteSpace(row.TraceId))
                        existing.TraceId = row.TraceId;
                    if (!string.IsNullOrWhiteSpace(row.Timezone))
                        existing.Timezone = row.Timezone;

                    // Перезаписываем/дополняем доп. поля.
                    if (mergedAdditional.Count > 0)
                    {
                        var ordered = new SortedDictionary<string, string>(mergedAdditional, StringComparer.OrdinalIgnoreCase);
                        existing.AdditionalFields = ordered;
                    }

                    existing.IsBlocked = row.IsBlocked;
                }

                // Сохраняем изменения по одной строке, чтобы ловить ошибки построчно и запускать AuditingInterceptor.
                await _tenantDb.SaveChangesAsync(ct);

                results.Add(new ClientImportRowResult { RowIndex = row.RowIndex, Error = null });
            }
            catch (Exception ex)
            {
                results.Add(new ClientImportRowResult { RowIndex = row.RowIndex, Error = ex.Message });
            }
        }

        return new ClientImportResult { RowResults = results };
    }

    /// <summary>Нормализует телефон: убирает скобки, тире, пробелы; 8/7 в начале заменяет на +7.</summary>
    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var cleaned = Regex.Replace(phone, "[^0-9+]", "");

        if (cleaned.StartsWith("+7", StringComparison.Ordinal))
            return cleaned;

        if (cleaned.StartsWith("8", StringComparison.Ordinal) || cleaned.StartsWith("7", StringComparison.Ordinal))
            return "+7" + cleaned.Substring(1);

        return cleaned;
    }
}
