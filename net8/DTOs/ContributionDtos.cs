using System;
using System.Text.Json.Serialization;
namespace Shagun.DTOs
{
    public class ContributionCreateDto
    {
        public decimal Amount { get; set; }
        [JsonPropertyName("invitee_token")]
        public string? InviteeToken { get; set; }
        public bool Anonymous { get; set; }
    }

    public class ContributionResponseDto
    {
        public int Id { get; set; }
        [JsonPropertyName("gift_item_id")]
        public int GiftItemId { get; set; }
        public decimal Amount { get; set; }
        public bool Anonymous { get; set; }
        public string? Status { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("invitee_name")]
        public string? InviteeName { get; set; }
    }
}
