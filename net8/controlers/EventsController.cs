using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using Shagun.DTOs;
using Shagun.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Shagun.Controlers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        private readonly Shagun.Services.CurrentUserService _currentUserService;

        public EventsController(ApplicationDbContext db, Shagun.Services.CurrentUserService currentUserService)
        {
            _db = db;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto dto)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var ev = new Event
            {
                HostId = currentUser.Id,
                EventName = dto.EventName,
                GroomName = dto.GroomName,
                BrideName = dto.BrideName,
                EventDate = dto.EventDate ?? DateOnly.FromDateTime(System.DateTime.UtcNow),
                Venue = dto.Venue,
                Description = dto.Description,
                CoverPhotoUrl = dto.CoverPhotoUrl,
                Status = dto.Status ?? "active"
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            var res = new EventResponseDto
            {
                Id = ev.Id,
                HostId = ev.HostId,
                EventName = ev.EventName,
                GroomName = ev.GroomName,
                BrideName = ev.BrideName,
                EventDate = ev.EventDate,
                Venue = ev.Venue,
                Description = ev.Description,
                CoverPhotoUrl = ev.CoverPhotoUrl,
                Status = ev.Status,
                CreatedAt = ev.CreatedAt
            };

            return CreatedAtAction(nameof(GetEvent), new { eventId = res.Id }, res);
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetUserEvents()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var list = await _db.Events.Where(e => e.HostId == currentUser.Id).ToListAsync();
            var res = list.Select(e => new EventResponseDto
            {
                Id = e.Id,
                HostId = e.HostId,
                EventName = e.EventName,
                GroomName = e.GroomName,
                BrideName = e.BrideName,
                EventDate = e.EventDate,
                Venue = e.Venue,
                Description = e.Description,
                CoverPhotoUrl = e.CoverPhotoUrl,
                Status = e.Status,
                CreatedAt = e.CreatedAt
            });
            return Ok(res);
        }

        [HttpGet("{eventId}")]
        public async Task<IActionResult> GetEvent(int eventId)
        {
            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { detail = "Event not found" });

            var res = new EventResponseDto
            {
                Id = ev.Id,
                HostId = ev.HostId,
                EventName = ev.EventName,
                GroomName = ev.GroomName,
                BrideName = ev.BrideName,
                EventDate = ev.EventDate,
                Venue = ev.Venue,
                Description = ev.Description,
                CoverPhotoUrl = ev.CoverPhotoUrl,
                Status = ev.Status,
                CreatedAt = ev.CreatedAt
            };
            return Ok(res);
        }

        [HttpPut("{eventId}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdateEvent(int eventId, [FromBody] EventUpdateDto dto)
        {
            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { detail = "Event not found" });

            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();
            if (ev.HostId != currentUser.Id) return Forbid();

            if (dto.EventName != null) ev.EventName = dto.EventName;
            if (dto.GroomName != null) ev.GroomName = dto.GroomName;
            if (dto.BrideName != null) ev.BrideName = dto.BrideName;
            if (dto.EventDate.HasValue) ev.EventDate = dto.EventDate.Value;
            if (dto.Venue != null) ev.Venue = dto.Venue;
            if (dto.Description != null) ev.Description = dto.Description;
            if (dto.CoverPhotoUrl != null) ev.CoverPhotoUrl = dto.CoverPhotoUrl;
            if (dto.Status != null) ev.Status = dto.Status;

            await _db.SaveChangesAsync();

            var res = new EventResponseDto
            {
                Id = ev.Id,
                HostId = ev.HostId,
                EventName = ev.EventName,
                GroomName = ev.GroomName,
                BrideName = ev.BrideName,
                EventDate = ev.EventDate,
                Venue = ev.Venue,
                Description = ev.Description,
                CoverPhotoUrl = ev.CoverPhotoUrl,
                Status = ev.Status,
                CreatedAt = ev.CreatedAt
            };

            return Ok(res);
        }

        [HttpDelete("{eventId}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { detail = "Event not found" });
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();
            if (ev.HostId != currentUser.Id) return Forbid();

            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
