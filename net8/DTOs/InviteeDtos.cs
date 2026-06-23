using System;
using System.Text.Json.Serialization;

namespace Shagun.DTOs
{
    public class InviteeCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Relation { get; set; }
    }

    public class InviteeResponseDto
    {
        public int Id { get; set; }
        [JsonPropertyName("event_id")]
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Relation { get; set; }
        [JsonPropertyName("invite_token")]
        public string InviteToken { get; set; } = string.Empty;
        public string? Status { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
