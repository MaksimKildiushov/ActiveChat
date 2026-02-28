using System.Text.RegularExpressions;
using Ac.Application.Contracts.Models;
using Ac.Application.Services;
using Ac.Data;
using Ac.Data.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ac.Api.Controllers;

/// <summary>
/// Админские операции с клиентами тенанта. Требует роль Admin.
/// Перед каждой операцией схема тенанта задаётся через <see cref="CurrentTenantContext"/>.
/// </summary>
[ApiController]
[Route("tenants/{tenantId:int}/clients")]
//[AllowAnonymous]
[Authorize(Roles = "Admin")]
public class ClientsController(
    ApiDb apiDb,
    TenantDb tenantDb,
    CurrentTenantContext tenantContext,
    ClientService clientService) : ControllerBase
{
    private static readonly Regex SchemaNameRegex = new(@"^t_[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    /// <summary>Возвращает схему по tenantId (t_ + Inn) или null, если тенант не найден или Inn невалиден.</summary>
    private async Task<string?> GetSchemaNameAsync(int tenantId, CancellationToken ct)
    {
        var tenant = await apiDb.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId && t.Inn != null && t.Inn != "")
            .Select(t => new { t.Inn })
            .FirstOrDefaultAsync(ct);

        if (tenant is null)
            return null;

        var schemaName = "t_" + tenant.Inn!.Trim();
        if (!SchemaNameRegex.IsMatch(schemaName))
            return null;

        return schemaName;
    }

    /// <summary>Список клиентов выбранного тенанта.</summary>
    [HttpGet]
    public async Task<ActionResult<List<AdminClientDto>>> GetClients(int tenantId, CancellationToken ct)
    {
        var schemaName = await GetSchemaNameAsync(tenantId, ct);
        if (schemaName is null)
            return NotFound(new { error = "Тенант не найден или неверный ИНН." });

        tenantContext.Set(tenantId, schemaName);

        var entities = await tenantDb.Clients
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .ToListAsync(ct);

        var list = entities.Select(c => new AdminClientDto(
            c.Id,
            c.ChannelUserId,
            c.OverrideUserId,
            c.DisplayName,
            c.Email,
            c.Phone,
            c.TraceId,
            c.Timezone,
            c.IsBlocked,
            (IReadOnlyDictionary<string, string>)c.AdditionalFields)).ToList();

        return Ok(list);
    }

    /// <summary>Обновление клиента.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateClient(
        int tenantId,
        int id,
        [FromBody] AdminClientUpdateRequest request,
        CancellationToken ct)
    {
        var schemaName = await GetSchemaNameAsync(tenantId, ct);
        if (schemaName is null)
            return NotFound(new { error = "Тенант не найден или неверный ИНН." });

        tenantContext.Set(tenantId, schemaName);

        var entity = await tenantDb.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return NotFound(new { error = $"Клиент с Id={id} не найден." });

        entity.ChannelUserId = request.ChannelUserId;
        entity.OverrideUserId = request.OverrideUserId;
        entity.DisplayName = request.DisplayName;
        entity.Email = request.Email;
        entity.Phone = request.Phone;
        entity.TraceId = request.TraceId;
        entity.Timezone = request.Timezone;
        entity.IsBlocked = request.IsBlocked;

        if (request.AdditionalFields.Count > 0)
        {
            var dict = new Dictionary<string, string>(request.AdditionalFields, StringComparer.OrdinalIgnoreCase);
            var ordered = new SortedDictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
            entity.AdditionalFields = ordered;
        }
        else
        {
            entity.AdditionalFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        await tenantDb.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Удаление клиента.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteClient(int tenantId, int id, CancellationToken ct)
    {
        var schemaName = await GetSchemaNameAsync(tenantId, ct);
        if (schemaName is null)
            return NotFound(new { error = "Тенант не найден или неверный ИНН." });

        tenantContext.Set(tenantId, schemaName);

        var entity = await tenantDb.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return NotFound(new { error = $"Клиент с Id={id} не найден." });

        tenantDb.Clients.Remove(entity);
        await tenantDb.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Импорт клиентов из списка строк (например, из разобранного XLSX).</summary>
    [HttpPost("import")]
    public async Task<ActionResult<AdminClientImportResultDto>> ImportClients(
        int tenantId,
        [FromBody] List<AdminClientImportRowDto> rows,
        CancellationToken ct)
    {
        var schemaName = await GetSchemaNameAsync(tenantId, ct);
        if (schemaName is null)
            return NotFound(new { error = "Тенант не найден или неверный ИНН." });

        tenantContext.Set(tenantId, schemaName);

        var importRows = rows.ConvertAll(dto => new ClientImportRow
        {
            RowIndex = dto.RowIndex,
            ChannelUserId = dto.ChannelUserId,
            OverrideUserId = dto.OverrideUserId,
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            Phone = dto.Phone,
            TraceId = dto.TraceId,
            Timezone = dto.Timezone,
            IsBlocked = dto.IsBlocked,
            AdditionalFields = dto.AdditionalFields
        });

        var result = await clientService.ImportClientsAsync(importRows, ct);

        var dtoResult = new AdminClientImportResultDto(
            result.RowResults.Select(r => new AdminClientImportRowResultDto(r.RowIndex, r.Error)).ToList());

        return Ok(dtoResult);
    }
}
