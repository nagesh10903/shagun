using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using Shagun.DTOs;
using Shagun.Models;
using System.Threading.Tasks;
using System.Linq;

namespace Shagun.Controlers
{
    [ApiController]
    public class GiftsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        private readonly Shagun.Services.CurrentUserService _currentUserService;

        public GiftsController(ApplicationDbContext db, Shagun.Services.CurrentUserService currentUserService)
        {
            _db = db;
            _currentUserService = currentUserService;
        }

        [HttpPost("/api/events/{eventId}/gifts")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> CreateGift(int eventId, [FromBody] GiftCreateDto dto)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null || ev.HostId != currentUser.Id) return NotFound(new { detail = "Event not found or unauthorized" });

            var gift = new GiftItem
            {
                EventId = eventId,
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                EstimatedCost = dto.EstimatedCost ?? 0M,
                ContributedAmount = 0M,
                Status = "OPEN"
            };

            _db.GiftItems.Add(gift);
            await _db.SaveChangesAsync();

            var res = new GiftResponseDto
            {
                Id = gift.Id,
                EventId = gift.EventId,
                Name = gift.Name,
                Description = gift.Description,
                ImageUrl = gift.ImageUrl,
                EstimatedCost = gift.EstimatedCost,
                ContributedAmount = gift.ContributedAmount,
                Status = gift.Status,
                CreatedAt = gift.CreatedAt
            };

            return CreatedAtAction(nameof(GetGift), new { giftId = res.Id }, res);
        }

        [HttpGet("/api/events/{eventId}/gifts")]
        public async Task<IActionResult> ListGifts(int eventId)
        {
            var gifts = await _db.GiftItems.Where(g => g.EventId == eventId).ToListAsync();
            var res = gifts.Select(g => new GiftResponseDto
            {
                Id = g.Id,
                EventId = g.EventId,
                Name = g.Name,
                Description = g.Description,
                ImageUrl = g.ImageUrl,
                EstimatedCost = g.EstimatedCost,
                ContributedAmount = g.ContributedAmount,
                Status = g.Status,
                CreatedAt = g.CreatedAt
            });
            return Ok(res);
        }

        [HttpGet("/api/gifts/{giftId}")]
        public async Task<IActionResult> GetGift(int giftId)
        {
            var gift = await _db.GiftItems.FindAsync(giftId);
            if (gift == null) return NotFound(new { detail = "Gift item not found" });
            var res = new GiftResponseDto
            {
                Id = gift.Id,
                EventId = gift.EventId,
                Name = gift.Name,
                Description = gift.Description,
                ImageUrl = gift.ImageUrl,
                EstimatedCost = gift.EstimatedCost,
                ContributedAmount = gift.ContributedAmount,
                Status = gift.Status,
                CreatedAt = gift.CreatedAt
            };
            return Ok(res);
        }

        [HttpPut("/api/gifts/{giftId}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdateGift(int giftId, [FromBody] GiftCreateDto dto)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var gift = await _db.GiftItems.FindAsync(giftId);
            if (gift == null) return NotFound(new { detail = "Gift item not found" });
            var ev = await _db.Events.FindAsync(gift.EventId);
            if (ev == null || ev.HostId != currentUser.Id) return Forbid();

            gift.Name = dto.Name ?? gift.Name;
            gift.Description = dto.Description ?? gift.Description;
            gift.ImageUrl = dto.ImageUrl ?? gift.ImageUrl;
            if (dto.EstimatedCost.HasValue) gift.EstimatedCost = dto.EstimatedCost.Value;

            if (gift.ContributedAmount >= gift.EstimatedCost) gift.Status = "FUNDED";
            else if (gift.Status == "FUNDED" && gift.ContributedAmount < gift.EstimatedCost) gift.Status = "OPEN";

            await _db.SaveChangesAsync();

            var res = new GiftResponseDto
            {
                Id = gift.Id,
                EventId = gift.EventId,
                Name = gift.Name,
                Description = gift.Description,
                ImageUrl = gift.ImageUrl,
                EstimatedCost = gift.EstimatedCost,
                ContributedAmount = gift.ContributedAmount,
                Status = gift.Status,
                CreatedAt = gift.CreatedAt
            };

            return Ok(res);
        }

        [HttpDelete("/api/gifts/{giftId}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> DeleteGift(int giftId)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var gift = await _db.GiftItems.FindAsync(giftId);
            if (gift == null) return NotFound(new { detail = "Gift item not found" });
            var ev = await _db.Events.FindAsync(gift.EventId);
            if (ev == null || ev.HostId != currentUser.Id) return Forbid();

            _db.GiftItems.Remove(gift);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
