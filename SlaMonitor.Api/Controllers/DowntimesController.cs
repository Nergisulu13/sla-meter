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

        [HttpGet]
        [Authorize(
            AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
            Policy = "DowntimesRead"
        )]
        public async Task<IActionResult> Get()
        {
            var items = await _db.Downtimes
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
            var item = await _db.Downtimes.FindAsync(id);
            if (item is null) return NotFound();

            item.Environment = dto.Environment;
            item.DurationMinutes = dto.DurationMinutes;
            item.Customers = dto.Customers;
            item.Reason = dto.Reason;
            item.OccurredAt = dto.OccurredAt;

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
            var item = await _db.Downtimes.FindAsync(id);
            if (item is null) return NotFound();

            _db.Downtimes.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}