using System;

namespace Shagun.DTOs
{
    public class EventCreateDto
    {
        public int HostId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? GroomName { get; set; }
        public string? BrideName { get; set; }
        public DateOnly? EventDate { get; set; }
        public string? Venue { get; set; }
        public string? Description { get; set; }
        public string? CoverPhotoUrl { get; set; }
        public string? Status { get; set; }
    }

    public class EventUpdateDto
    {
        public string? EventName { get; set; }
        public string? GroomName { get; set; }
        public string? BrideName { get; set; }
        public DateOnly? EventDate { get; set; }
        public string? Venue { get; set; }
        public string? Description { get; set; }
        public string? CoverPhotoUrl { get; set; }
        public string? Status { get; set; }
    }

    public class EventResponseDto
    {
        public int Id { get; set; }
        public int HostId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? GroomName { get; set; }
        public string? BrideName { get; set; }
        public DateOnly? EventDate { get; set; }
        public string? Venue { get; set; }
        public string? Description { get; set; }
        public string? CoverPhotoUrl { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
