using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Shagun.DBRepo;
using Shagun.Models;
using Shagun.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shagun.Controlers
{
    [ApiController]
    [Route("api/events/{eventId}/invitees")]
    public class InviteesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly Shagun.Services.CurrentUserService _currentUserService;

        public InviteesController(ApplicationDbContext db, Shagun.Services.CurrentUserService currentUserService)
        {
            _db = db;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> AddInvitee(int eventId, [FromBody] Invitee inviteeData)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null || ev.HostId != currentUser.Id) return NotFound(new { detail = "Event not found or unauthorized" });

            var token = System.Guid.NewGuid().ToString("N");
            var inv = new Invitee
            {
                EventId = eventId,
                Name = inviteeData.Name,
                Phone = inviteeData.Phone,
                Email = inviteeData.Email,
                Relation = inviteeData.Relation,
                InviteToken = token,
                Status = "sent"
            };

            _db.Invitees.Add(inv);
            await _db.SaveChangesAsync();

            var res = new InviteeResponseDto
            {
                Id = inv.Id,
                EventId = inv.EventId,
                Name = inv.Name,
                Phone = inv.Phone,
                Email = inv.Email,
                Relation = inv.Relation,
                InviteToken = inv.InviteToken,
                Status = inv.Status,
                CreatedAt = inv.CreatedAt
            };

            return Created(string.Empty, res);
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetInvitees(int eventId)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null || ev.HostId != currentUser.Id) return NotFound(new { detail = "Event not found or unauthorized" });

            var list = await _db.Invitees.Where(i => i.EventId == eventId).ToListAsync();
            var res = list.Select(i => new InviteeResponseDto
            {
                Id = i.Id,
                EventId = i.EventId,
                Name = i.Name,
                Phone = i.Phone,
                Email = i.Email,
                Relation = i.Relation,
                InviteToken = i.InviteToken,
                Status = i.Status,
                CreatedAt = i.CreatedAt
            });
            return Ok(res);
        }

        [HttpPost("/api/events/{eventId}/invitees/upload")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> BulkUploadInvitees(int eventId, IFormFile file)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null || ev.HostId != currentUser.Id) return NotFound(new { detail = "Event not found or unauthorized" });

            if (file == null) return BadRequest(new { detail = "File missing" });

            List<Invitee> added = new List<Invitee>();
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                var content = Encoding.UTF8.GetString(ms.ToArray());
                using var reader = new StringReader(content);
                var header = reader.ReadLine();
                if (header == null) return BadRequest(new { detail = "Empty CSV" });
                var headers = header.Split(',');
                // normalize
                for (int i = 0; i < headers.Length; i++) headers[i] = headers[i].Trim().ToLower();

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var cols = line.Split(',');
                    var dict = new Dictionary<string, string>();
                    for (int i = 0; i < headers.Length && i < cols.Length; i++) dict[headers[i]] = cols[i].Trim();

                    if (!dict.ContainsKey("name") || !dict.ContainsKey("phone")) continue;

                    var inv = new Invitee
                    {
                        EventId = eventId,
                        Name = dict["name"],
                        Phone = dict["phone"],
                        Email = dict.ContainsKey("email") ? dict["email"] : null,
                        Relation = dict.ContainsKey("relation") ? dict["relation"] : null,
                        InviteToken = System.Guid.NewGuid().ToString("N"),
                        Status = "sent"
                    };
                    _db.Invitees.Add(inv);
                    added.Add(inv);
                }
            }

            if (added.Count == 0) return BadRequest(new { detail = "No valid invitee records found in CSV" });

            await _db.SaveChangesAsync();
            var resList = added.Select(i => new InviteeResponseDto
            {
                Id = i.Id,
                EventId = i.EventId,
                Name = i.Name,
                Phone = i.Phone,
                Email = i.Email,
                Relation = i.Relation,
                InviteToken = i.InviteToken,
                Status = i.Status,
                CreatedAt = i.CreatedAt
            });
            return Created(string.Empty, resList);
        }

        [HttpDelete("/{inviteeId}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> RemoveInvitee(int eventId, int inviteeId)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null || ev.HostId != currentUser.Id) return NotFound(new { detail = "Event not found or unauthorized" });

            var invitee = await _db.Invitees.FirstOrDefaultAsync(i => i.Id == inviteeId && i.EventId == eventId);
            if (invitee == null) return NotFound(new { detail = "Invitee not found" });

            _db.Invitees.Remove(invitee);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // Public: get invitee by token
        [HttpGet("/api/events/{eventId}/invitees/token/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInviteeByToken(string token)
        {
            var invitee = await _db.Invitees.FirstOrDefaultAsync(i => i.InviteToken == token);
            if (invitee == null) return NotFound(new { detail = "Invalid invitation token" });

            if (invitee.Status == "sent")
            {
                invitee.Status = "opened";
                await _db.SaveChangesAsync();
                await _db.Entry(invitee).ReloadAsync();
            }

            var res = new InviteeResponseDto
            {
                Id = invitee.Id,
                EventId = invitee.EventId,
                Name = invitee.Name,
                Phone = invitee.Phone,
                Email = invitee.Email,
                Relation = invitee.Relation,
                InviteToken = invitee.InviteToken,
                Status = invitee.Status,
                CreatedAt = invitee.CreatedAt
            };
            return Ok(res);
        }
    }
}
