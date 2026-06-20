using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using Shagun.DTOs;
using Shagun.Models;
using System.Security.Cryptography;
using System.Text;

namespace Shagun.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _db;

        public AuthService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<User?> CreateUserAsync(UserCreateDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Phone == dto.Phone || u.Email == dto.Email)) return null;

            var user = new User
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Email = dto.Email,
                PasswordHash = HashPassword(dto.Password),
                Role = "host"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<User?> AuthenticateUserAsync(string phone, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == phone);
            if (user == null) return null;
            if (!VerifyPassword(password, user.PasswordHash)) return null;
            return user;
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
