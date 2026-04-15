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

        private Guid GetTenantId()
        {
            var tenantClaim = User.Claims.FirstOrDefault(x => x.Type == "tenant_id");

            if (tenantClaim == null || string.IsNullOrWhiteSpace(tenantClaim.Value))
                throw new UnauthorizedAccessException("Tenant bilgisi bulunamadı.");

            return Guid.Parse(tenantClaim.Value);
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
            IQueryable<DowntimeRecord> query = _db.Downtimes;

            if (!IsSuperAdmin())
            {
                var tenantId = GetTenantId();
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
            dto.Id = Guid.NewGuid();

            if (!IsSuperAdmin())
            {
                dto.TenantId = GetTenantId();
            }

            _db.Downtimes.Add(dto);
            await _db.SaveChangesAsync();

            return Ok(dto);
        }

        [HttpPut("{id:guid}")]
        [Authorize(
            AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
            Policy = "DowntimesWrite"
        )]
        public async Task<IActionResult> Update(Guid id, [FromBody] DowntimeRecord dto)
        {
            var item = await _db.Downtimes.FirstOrDefaultAsync(x => x.Id == id);
            if (item is null)
                return NotFound();

            if (!IsSuperAdmin())
            {
                var tenantId = GetTenantId();

                if (item.TenantId != tenantId)
                    return Forbid();

                dto.TenantId = tenantId;
            }

            item.Environment = dto.Environment;
            item.DurationMinutes = dto.DurationMinutes;
            item.Customers = dto.Customers;
            item.Reason = dto.Reason;
            item.OccurredAt = dto.OccurredAt;

            if (IsSuperAdmin())
            {
                item.TenantId = dto.TenantId;
            }

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
                var tenantId = GetTenantId();

                if (item.TenantId != tenantId)
                    return Forbid();
            }

            _db.Downtimes.Remove(item);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}