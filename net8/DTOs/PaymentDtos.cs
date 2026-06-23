using System;
using System.Text.Json.Serialization;
namespace Shagun.DTOs
{
    public class PaymentVerificationDto
    {
        [JsonPropertyName("razorpay_order_id")]
        public string RazorpayOrderId { get; set; } = string.Empty;
        [JsonPropertyName("razorpay_payment_id")]
        public string RazorpayPaymentId { get; set; } = string.Empty;
        [JsonPropertyName("razorpay_signature")]
        public string RazorpaySignature { get; set; } = string.Empty;
        [JsonPropertyName("contribution_id")]   
        public int ContributionId { get; set; }
    }
}
