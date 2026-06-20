using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Shagun.Services;
using Shagun.DTOs;
using Shagun.DBRepo;
using Shagun.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System;

namespace Shagun.Controlers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserCreateDto dto)
        {
            var exists = await _db.Users.AnyAsync(u => u.Phone == dto.Phone || u.Email == dto.Email);
            if (exists) return BadRequest(new { detail = "User already exists" });

            var user = new User
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Email = dto.Email,
                PasswordHash = HashPassword(dto.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var res = new Shagun.DTOs.UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Phone = user.Phone,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Created(string.Empty, res);
        }

        [HttpGet("check-phone")]
        public async Task<IActionResult> CheckPhone([FromQuery] string phone)
        {
            if (string.IsNullOrEmpty(phone)) return BadRequest(new { detail = "phone query required" });
            var exists = await _db.Users.AnyAsync(u => u.Phone == phone);
            return Ok(new { available = !exists });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] LoginRequestDto login)
        {
       
            // username is phone 
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == login.Username);
            if (user == null) return Unauthorized(new { detail = "Invalid credentials" });

            //if (!VerifyPassword(password, user.PasswordHash)) return Unauthorized(new { detail = "Invalid credentials" });

            var access = JwtHelper.CreateAccessToken(user.Phone, user.Role ?? "user", _config);
            var refresh = "refresh_mock_token"; // Implement refresh tokens later

            var userDto = new Shagun.DTOs.UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Phone = user.Phone,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            var resp = new {
                access_token = access,
                refresh_token = refresh,
                token_type = "bearer",
                user = userDto
            };

            return Ok(resp);
        }
        

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            return HashPassword(password) == storedHash;
        }
    }
}
