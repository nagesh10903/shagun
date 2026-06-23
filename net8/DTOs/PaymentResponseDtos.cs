using System;
using System.Text.Json.Serialization;

namespace Shagun.DTOs
{
    public class PaymentCreateDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
    }

    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public string Gateway { get; set; } = string.Empty;
        [JsonPropertyName("gateway_order_id")]
        public string GatewayOrderId { get; set; } = string.Empty;
        [JsonPropertyName("gateway_payment_id")]
        public string? GatewayPaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? Status { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
    }

    public class RazorpayOrderResponseDto
    {
       
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        [JsonPropertyName("key_id")]
        public string KeyId { get; set; } = string.Empty;
    }
}
