using System;
using System.Text.Json.Serialization;

namespace Shagun.DTOs
{
    public class GiftCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
        [JsonPropertyName("estimated_cost")]
        public decimal? EstimatedCost { get; set; }
    }

    public class GiftResponseDto
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
        [JsonPropertyName("estimated_cost")]
        public decimal? EstimatedCost { get; set; }
        [JsonPropertyName("contributed_amount")]
        public decimal? ContributedAmount { get; set; }
        public string? Status { get; set; }
        [JsonPropertyName("created_at")]    
        public DateTime? CreatedAt { get; set; }
    }
}
