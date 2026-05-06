using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using SlaMonitor.Api.Data;
using SlaMonitor.Api.Models;

namespace SlaMonitor.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DowntimesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DowntimesController(AppDbContext db)
        {
            _db = db;
        }

        private bool TryGetTenantId(out Guid tenantId)
        {
            tenantId = Guid.Empty;

            var tenantClaim = User.Claims.FirstOrDefault(x => x.Type == "tenant_id");
            if (tenantClaim == null || string.IsNullOrWhiteSpace(tenantClaim.Value))
                return false;

            return Guid.TryParse(tenantClaim.Value, out tenantId);
        }

        private bool TryGetTenantName(out string tenantName)
        {
            tenantName = string.Empty;

            var tenantClaim = User.Claims.FirstOrDefault(x => x.Type == "tenant_name");
            if (tenantClaim == null || string.IsNullOrWhiteSpace(tenantClaim.Value))
                return false;

            tenantName = tenantClaim.Value;
            return true;
        }

        private bool IsSuperAdmin()
        {
            return User.Claims.Any(x =>
                (x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" ||
                 x.Type == "role") &&
                x.Value == "SuperAdmin");
        }

        [HttpGet]
        [Authorize(
            AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
            Policy = "DowntimesRead"
        )]
        public async Task<IActionResult> Get()
        {
            IQueryable<DowntimeRecord> query = _db.Downtimes.AsNoTracking();

            if (!IsSuperAdmin())
            {
                if (!TryGetTenantId(out var tenantId))
                    return Unauthorized("Tenant bilgisi bulunamadı.");

                query = query.Where(x => x.TenantId == tenantId);
            }

            var items = await query
                .OrderByDescending(x => x.OccurredAt)
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        [Authorize(
            AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
            Policy = "DowntimesWrite"
        )]
        public async Task<IActionResult> Create([FromBody] DowntimeRecord dto)
        {
            if (dto == null)
                return BadRequest("Geçersiz veri gönderildi.");

            if (dto.DurationMinutes < 0)
                return BadRequest("DurationMinutes negatif olamaz.");

            var entity = new DowntimeRecord
            {
                Id = Guid.NewGuid(),
                DurationMinutes = dto.DurationMinutes,
                Customers = dto.Customers ?? string.Empty,
                Reason = dto.Reason ?? string.Empty,
                OccurredAt = dto.OccurredAt == default ? DateTime.UtcNow : dto.OccurredAt
            };

            if (!IsSuperAdmin())
            {
                if (!TryGetTenantId(out var tenantId))
                    return Unauthorized("Tenant bilgisi bulunamadı.");

                if (!TryGetTenantName(out var tenantName))
                    return Unauthorized("Tenant adı bulunamadı.");

                entity.TenantId = tenantId;
                entity.Environment = tenantName;
            }
            else
            {
                if (dto.TenantId == Guid.Empty)
                    return BadRequest("SuperAdmin için TenantId zorunludur.");

                if (string.IsNullOrWhiteSpace(dto.Environment))
                    return BadRequest("SuperAdmin için Environment zorunludur.");

                entity.TenantId = dto.TenantId;
                entity.Environment = dto.Environment;
            }

            _db.Downtimes.Add(entity);
            await _db.SaveChangesAsync();

            return Ok(entity);
        }

        [HttpPut("{id:guid}")]
        [Authorize(
            AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
            Policy = "DowntimesWrite"
        )]
        public async Task<IActionResult> Update(Guid id, [FromBody] DowntimeRecord dto)
        {
            if (dto == null)
                return BadRequest("Geçersiz veri gönderildi.");

            if (dto.DurationMinutes < 0)
                return BadRequest("DurationMinutes negatif olamaz.");

            var item = await _db.Downtimes.FirstOrDefaultAsync(x => x.Id == id);
            if (item is null)
                return NotFound();

            if (!IsSuperAdmin())
            {
                if (!TryGetTenantId(out var tenantId))
                    return Unauthorized("Tenant bilgisi bulunamadı.");

                if (!TryGetTenantName(out var tenantName))
                    return Unauthorized("Tenant adı bulunamadı.");

                if (item.TenantId != tenantId)
                    return Forbid();

                item.TenantId = tenantId;
                item.Environment = tenantName;
            }
            else
            {
                if (dto.TenantId == Guid.Empty)
                    return BadRequest("SuperAdmin için TenantId zorunludur.");

                if (string.IsNullOrWhiteSpace(dto.Environment))
                    return BadRequest("SuperAdmin için Environment zorunludur.");

                item.TenantId = dto.TenantId;
                item.Environment = dto.Environment;
            }

            item.DurationMinutes = dto.DurationMinutes;
            item.Customers = dto.Customers ?? string.Empty;
            item.Reason = dto.Reason ?? string.Empty;
            item.OccurredAt = dto.OccurredAt == default ? item.OccurredAt : dto.OccurredAt;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(
            AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
            Policy = "DowntimesDelete"
        )]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _db.Downtimes.FirstOrDefaultAsync(x => x.Id == id);
            if (item is null)
                return NotFound();

            if (!IsSuperAdmin())
            {
                if (!TryGetTenantId(out var tenantId))
                    return Unauthorized("Tenant bilgisi bulunamadı.");

                if (item.TenantId != tenantId)
                    return Forbid();
            }

            _db.Downtimes.Remove(item);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}