using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using System.Threading.Tasks;
using System.Linq;

namespace Shagun.Controlers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly Shagun.Services.CurrentUserService _currentUserService;

        public AdminController(ApplicationDbContext db, Shagun.Services.CurrentUserService currentUserService)
        {
            _db = db;
            _currentUserService = currentUserService;
        }

        private async Task<bool> IsAdminAsync()
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            return user != null && (user.Role == "admin");
        }

        [HttpGet("/api/admin/users")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetAllUsers()
        {
            if (!await IsAdminAsync()) return Forbid();
            var users = await _db.Users.ToListAsync();
            var res = users.Select(u => new Shagun.DTOs.UserResponseDto
            {
                Id = u.Id,
                Name = u.Name,
                Phone = u.Phone,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            });
            return Ok(res);
        }

        [HttpGet("/api/admin/events")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetAllEvents()
        {
            if (!await IsAdminAsync()) return Forbid();
            var events = await _db.Events.ToListAsync();
            var resEv = events.Select(e => new Shagun.DTOs.EventResponseDto
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
            return Ok(resEv);
        }

        [HttpGet("/api/admin/payments")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetAllPayments()
        {
            if (!await IsAdminAsync()) return Forbid();
            var payments = await _db.Payments.ToListAsync();
            var resP = payments.Select(p => new Shagun.DTOs.PaymentResponseDto
            {
                Id = p.Id,
                Gateway = p.Gateway,
                GatewayOrderId = p.GatewayOrderId ?? string.Empty,
                GatewayPaymentId = p.GatewayPaymentId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                CreatedAt = p.CreatedAt
            });
            return Ok(resP);
        }

        [HttpPut("/api/admin/users/{userId}/role")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromQuery] string role)
        {
            if (!await IsAdminAsync()) return Forbid();
            if (role != "host" && role != "admin") return BadRequest(new { detail = "Invalid role. Must be 'host' or 'admin'" });

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound(new { detail = "User not found" });
            user.Role = role;
            await _db.SaveChangesAsync();
            var resUser = new Shagun.DTOs.UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Phone = user.Phone,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
            return Ok(resUser);
        }
    }
}
