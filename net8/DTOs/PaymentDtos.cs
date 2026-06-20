using System;

namespace Shagun.DTOs
{
    public class PaymentVerificationDto
    {
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
        public int ContributionId { get; set; }
    }
}
