using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Shagun.DBRepo;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Shagun.Services
{
    public class CurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _db;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext db)
        {
            _httpContextAccessor = httpContextAccessor;
            _db = db;
        }

        public async Task<Shagun.Models.User?> GetCurrentUserAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated) return null;

            var phone = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (phone == null) return null;

            return await _db.Users.FirstOrDefaultAsync(u => u.Phone == phone);
        }
    }
}
