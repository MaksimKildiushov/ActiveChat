namespace Ac.Application.Contracts.Models;

/// <summary>
/// Клиент в разрезе админки (DTO для API и Admin UI).
/// </summary>
public sealed record AdminClientDto(
    int Id,
    string? ChannelUserId,
    string? OverrideUserId,
    string? DisplayName,
    string? Email,
    string? Phone,
    string? TraceId,
    string? Timezone,
    bool IsBlocked,
    IReadOnlyDictionary<string, string> AdditionalFields);

/// <summary>
/// Запрос на обновление клиента из админки.
/// </summary>
public sealed record AdminClientUpdateRequest(
    string? ChannelUserId,
    string? OverrideUserId,
    string? DisplayName,
    string? Email,
    string? Phone,
    string? TraceId,
    string? Timezone,
    bool IsBlocked,
    IReadOnlyDictionary<string, string> AdditionalFields);

/// <summary>
/// Одна строка импорта клиентов (между Admin и API).
/// </summary>
public sealed record AdminClientImportRowDto(
    int RowIndex,
    string? ChannelUserId,
    string? OverrideUserId,
    string? DisplayName,
    string? Email,
    string? Phone,
    string? TraceId,
    string? Timezone,
    bool IsBlocked,
    IReadOnlyDictionary<string, string> AdditionalFields);

/// <summary>
/// Результат обработки одной строки импорта.
/// </summary>
public sealed record AdminClientImportRowResultDto(
    int RowIndex,
    string? Error);

/// <summary>
/// Итоговый результат импорта клиентов.
/// </summary>
public sealed record AdminClientImportResultDto(
    IReadOnlyList<AdminClientImportRowResultDto> RowResults);

