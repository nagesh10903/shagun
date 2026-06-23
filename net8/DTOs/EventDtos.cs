using System;
using System.Text.Json.Serialization;

namespace Shagun.DTOs
{
    public class EventCreateDto
    {
        public int HostId { get; set; }
        [JsonPropertyName("event_name")]
        public string EventName { get; set; } = string.Empty;
        [JsonPropertyName("groom_name")]
        public string? GroomName { get; set; }
        [JsonPropertyName("bride_name")]    
        public string? BrideName { get; set; }
        [JsonPropertyName("event_date")]
        public DateOnly? EventDate { get; set; }
        public string? Venue { get; set; }
        public string? Description { get; set; }
        [JsonPropertyName("cover_photo_url")]
        public string? CoverPhotoUrl { get; set; }
        public string? Status { get; set; }
    }

    public class EventUpdateDto
    {
        [JsonPropertyName("event_name")]
        public string? EventName { get; set; }
        [JsonPropertyName("groom_name")]
        public string? GroomName { get; set; }
        [JsonPropertyName("bride_name")]
        public string? BrideName { get; set; }
        [JsonPropertyName("event_date")]
        public DateOnly? EventDate { get; set; }
        public string? Venue { get; set; }
        public string? Description { get; set; }

        [JsonPropertyName("cover_photo_url")]   
        public string? CoverPhotoUrl { get; set; }
        public string? Status { get; set; }
    }

    public class EventResponseDto
        {
        public int Id { get; set; }
        public int HostId { get; set; } 
        [JsonPropertyName("event_name")]
        public string EventName { get; set; } = string.Empty;
        [JsonPropertyName("groom_name")]
        public string? GroomName { get; set; }
        [JsonPropertyName("bride_name")]
        public string? BrideName { get; set; }
        [JsonPropertyName("event_date")]
        public DateOnly? EventDate { get; set; }
        public string? Venue { get; set; }
        public string? Description { get; set; }
        [JsonPropertyName("cover_photo_url")]
        public string? CoverPhotoUrl { get; set; }
        public string? Status { get; set; }
        [JsonPropertyName("created_at")]    
        public DateTime? CreatedAt { get; set; }
    }
}
