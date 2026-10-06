using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Data.Seeders;
using VoltaXApi.Dtos;
using VoltaXApi.Exceptions;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Controllers
{
    /// <summary>
    /// The default OCPP 2.0.1 settings profile that automatic provisioning sends to new charge points.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OcppDefaultConfigurationController : ControllerBase
    {
        private readonly VoltaXApiDbContext _db;

        public OcppDefaultConfigurationController(VoltaXApiDbContext db)
        {
            _db = db;
        }

        [HttpGet("GetProfile")]
        public async Task<ActionResult<List<OcppDefaultVariableDto>>> GetProfile()
        {
            // Covers a database migrated after the API started (the startup seed found no table).
            await OcppDefaultProfileSeeder.Seed(_db);
            return Ok(await LoadProfile());
        }

        /// <summary>Replaces the whole profile: rows missing from the list are removed.</summary>
        [HttpPut("SaveProfile")]
        public async Task<ActionResult<List<OcppDefaultVariableDto>>> SaveProfile(List<OcppDefaultVariableDto> profile)
        {
            Validate(profile);

            var existing = await _db.OcppDefaultVariables.ToListAsync();
            var kept = new HashSet<int>();
            for (var i = 0; i < profile.Count; i++)
            {
                var dto = profile[i];
                var row = dto.ID > 0 ? existing.FirstOrDefault(v => v.ID == dto.ID) : null;
                if (row == null)
                {
                    row = new OcppDefaultVariable();
                    _db.OcppDefaultVariables.Add(row);
                }
                else
                {
                    kept.Add(row.ID);
                }

                row.GroupName = string.IsNullOrWhiteSpace(dto.GroupName) ? "General" : dto.GroupName.Trim();
                row.ComponentName = dto.ComponentName.Trim();
                row.ComponentInstance = Blank(dto.ComponentInstance);
                row.EvseId = dto.EvseId;
                row.ConnectorId = dto.EvseId.HasValue ? dto.ConnectorId : null;
                row.VariableName = dto.VariableName.Trim();
                row.VariableInstance = Blank(dto.VariableInstance);
                row.AttributeType = Enum.Parse<AttributeEnumType>(dto.AttributeType, true);
                row.Value = dto.Value.Trim();
                row.Description = Blank(dto.Description);
                row.Enabled = dto.Enabled;
                row.SortOrder = (i + 1) * 10;
            }

            _db.OcppDefaultVariables.RemoveRange(existing.Where(v => !kept.Contains(v.ID)));
            await _db.SaveChangesAsync();
            return Ok(await LoadProfile());
        }

        /// <summary>Restores the built-in profile, dropping every edit.</summary>
        [HttpPost("ResetProfile")]
        public async Task<ActionResult<List<OcppDefaultVariableDto>>> ResetProfile()
        {
            _db.OcppDefaultVariables.RemoveRange(await _db.OcppDefaultVariables.ToListAsync());
            _db.OcppDefaultVariables.AddRange(OcppDefaultProfileSeeder.Defaults());
            await _db.SaveChangesAsync();
            return Ok(await LoadProfile());
        }

        private async Task<List<OcppDefaultVariableDto>> LoadProfile() =>
            (await _db.OcppDefaultVariables.AsNoTracking()
                .OrderBy(v => v.SortOrder).ThenBy(v => v.ID)
                .ToListAsync())
            .Select(v => new OcppDefaultVariableDto
            {
                ID = v.ID,
                GroupName = v.GroupName,
                ComponentName = v.ComponentName,
                ComponentInstance = v.ComponentInstance,
                EvseId = v.EvseId,
                ConnectorId = v.ConnectorId,
                VariableName = v.VariableName,
                VariableInstance = v.VariableInstance,
                AttributeType = v.AttributeType.ToString(),
                Value = v.Value,
                Description = v.Description,
                Enabled = v.Enabled,
                SortOrder = v.SortOrder
            })
            .ToList();

        private static void Validate(List<OcppDefaultVariableDto> profile)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var v in profile)
            {
                if (string.IsNullOrWhiteSpace(v.ComponentName) || string.IsNullOrWhiteSpace(v.VariableName))
                    throw new ValidationException("Every setting needs a component and a variable.");
                if (!Enum.TryParse<AttributeEnumType>(v.AttributeType, true, out _))
                    throw new ValidationException($"Unknown attribute type {v.AttributeType} for {v.ComponentName}.{v.VariableName}.");
                if (v.ConnectorId.HasValue && !v.EvseId.HasValue)
                    throw new ValidationException($"{v.ComponentName}.{v.VariableName}: a connector needs an EVSE.");

                var key = string.Join("|", v.ComponentName.Trim(), v.ComponentInstance?.Trim(), v.EvseId, v.ConnectorId,
                    v.VariableName.Trim(), v.VariableInstance?.Trim(), v.AttributeType);
                if (!seen.Add(key))
                    throw new ValidationException($"{Label(v)} appears twice in the profile.");
            }
        }

        private static string Label(OcppDefaultVariableDto v) =>
            $"{v.ComponentName}{(string.IsNullOrWhiteSpace(v.ComponentInstance) ? "" : $"[{v.ComponentInstance}]")}." +
            $"{v.VariableName}{(string.IsNullOrWhiteSpace(v.VariableInstance) ? "" : $"[{v.VariableInstance}]")}";

        private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
