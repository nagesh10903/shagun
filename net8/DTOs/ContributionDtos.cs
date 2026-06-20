using System;

namespace Shagun.DTOs
{
    public class ContributionCreateDto
    {
        public decimal Amount { get; set; }
        public string? InviteeToken { get; set; }
        public bool Anonymous { get; set; }
    }

    public class ContributionResponseDto
    {
        public int Id { get; set; }
        public int GiftItemId { get; set; }
        public decimal Amount { get; set; }
        public bool Anonymous { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? InviteeName { get; set; }
    }
}
