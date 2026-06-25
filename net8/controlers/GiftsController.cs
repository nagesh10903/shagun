using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using Shagun.DTOs;
using Shagun.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace Shagun.Controlers
{
    [ApiController]
    public class GiftsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        private readonly Shagun.Services.CurrentUserService _currentUserService;
        private readonly Microsoft.Extensions.Logging.ILogger<GiftsController> _logger;

        public GiftsController(ApplicationDbContext db, Shagun.Services.CurrentUserService currentUserService, Microsoft.Extensions.Logging.ILogger<GiftsController> logger)
        {
            _db = db;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpPost("/api/gifts/upload-image")]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<IActionResult> UploadGiftImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(new { detail = "No file uploaded" });

            try
            {
                var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "gifts");
                if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

                var ext = Path.GetExtension(file.FileName);
                var fileName = "gift_" + System.Guid.NewGuid().ToString("N") + ext;
                var filePath = Path.Combine(uploadsRoot, fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }

                var publicPath = "/uploads/gifts/" + fileName;
                _logger.LogInformation("Saved uploaded gift image: {FilePath} -> {PublicPath}", filePath, publicPath);
                return Ok(new { image_url = publicPath, file_name = fileName });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error saving uploaded gift image");
                return StatusCode(500, new { detail = "Failed to save uploaded file", error = ex.Message });
            }
        }

        [HttpPost("/api/events/{eventId}/gifts")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> CreateGift(int eventId, [FromBody] GiftCreateDto dto)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null || ev.HostId != currentUser.Id) return NotFound(new { detail = "Event not found or unauthorized" });

            _logger.LogInformation("CreateGift received DTO image_url: {ImageUrl}", dto.ImageUrl);

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

            _logger.LogInformation("Created Gift saved with ImageUrl: {ImageUrl} (Id: {Id})", gift.ImageUrl, gift.Id);

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

            _logger.LogInformation("UpdateGift incoming DTO image_url: {ImageUrl}", dto.ImageUrl);

            gift.Name = dto.Name ?? gift.Name;
            gift.Description = dto.Description ?? gift.Description;
            gift.ImageUrl = dto.ImageUrl ?? gift.ImageUrl;
            if (dto.EstimatedCost.HasValue) gift.EstimatedCost = dto.EstimatedCost.Value;

            if (gift.ContributedAmount >= gift.EstimatedCost) gift.Status = "FUNDED";
            else if (gift.Status == "FUNDED" && gift.ContributedAmount < gift.EstimatedCost) gift.Status = "OPEN";

            await _db.SaveChangesAsync();

            _logger.LogInformation("Updated Gift saved with ImageUrl: {ImageUrl} (Id: {Id})", gift.ImageUrl, gift.Id);

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
