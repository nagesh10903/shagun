using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Shagun.Controlers;
using Shagun.DBRepo;
using Shagun.Models;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Shagun.DTOs;

namespace Net8.Tests
{
    public class AuthControllerTests
    {
        private static string Hash(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        [Fact]
        public async Task Login_WithJsonCredentials_ReturnsAccessTokenAndUser()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var db = new ApplicationDbContext(options);
            var user = new User { Name = "Test", Phone = "+911234567890", Email = "t@test.com", PasswordHash = Hash("secret"), Role = "user" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var inMemorySettings = new Dictionary<string, string?> {
                {"Jwt:Key", "unittests-secret-key-which-is-long"},
                {"Jwt:Issuer", "test"},
                {"Jwt:Audience", "test"}
            };
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            var controller = new AuthController(db, config);

            var loginDto = new LoginRequestDto { Username = "+911234567890", Password = "secret" };
            var result = await controller.Login(loginDto);

            Assert.IsType<OkObjectResult>(result);
            var ok = result as OkObjectResult;
            Assert.NotNull(ok.Value);

            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("access_token", out _));
            Assert.True(doc.RootElement.TryGetProperty("user", out var userEl));
            Assert.Equal("+911234567890", userEl.GetProperty("Phone").GetString());
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var db = new ApplicationDbContext(options);
            var user = new User { Name = "Test", Phone = "+911234567891", Email = "t2@test.com", PasswordHash = Hash("secret"), Role = "user" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var inMemorySettings = new Dictionary<string, string?> {
                {"Jwt:Key", "unittests-secret-key-which-is-long"},
                {"Jwt:Issuer", "test"},
                {"Jwt:Audience", "test"}
            };
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            var controller = new AuthController(db, config);

            var loginDto = new LoginRequestDto { Username = "+911234567891", Password = "wrong" };
            var result = await controller.Login(loginDto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_WithFormData_ReturnsAccessTokenAndUser()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var db = new ApplicationDbContext(options);
            var phone = "+911234567892";
            var user = new User { Name = "FormTest", Phone = phone, Email = "t3@test.com", PasswordHash = Hash("secret"), Role = "user" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var inMemorySettings = new Dictionary<string, string?> {
                {"Jwt:Key", "unittests-secret-key-which-is-long"},
                {"Jwt:Issuer", "test"},
                {"Jwt:Audience", "test"}
            };
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            var controller = new AuthController(db, config);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            var form = new Dictionary<string, StringValues>
            {
                { "username", new StringValues(phone) },
                { "password", new StringValues("secret") }
            };
            httpContext.Request.Form = new FormCollection(form);
            controller.ControllerContext = new ControllerContext() { HttpContext = httpContext };

            var result = await controller.Login((Shagun.DTOs.LoginRequestDto?)null);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Login_AdminRole_ReturnsAdminUser()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var db = new ApplicationDbContext(options);
            var phone = "+911234567893";
            var user = new User { Name = "Admin", Phone = phone, Email = "admin@test.com", PasswordHash = Hash("secret"), Role = "admin" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Key", "unittests-secret-key-which-is-long"},
                {"Jwt:Issuer", "test"},
                {"Jwt:Audience", "test"}
            };
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            var controller = new AuthController(db, config);

            var loginDto = new LoginRequestDto { Username = phone, Password = "secret" };
            var result = await controller.Login(loginDto) as OkObjectResult;
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result.Value);
            using var doc = JsonDocument.Parse(json);
            var userEl = doc.RootElement.GetProperty("user");
            var role = userEl.GetProperty("Role").GetString();
            Assert.Equal("admin", role);
        }
    }
}
