using System;
using System.Text.Json.Serialization;
namespace Shagun.DTOs
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Role { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
