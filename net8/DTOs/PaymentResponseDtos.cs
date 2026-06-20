using System;

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
        public string GatewayOrderId { get; set; } = string.Empty;
        public string? GatewayPaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class RazorpayOrderResponseDto
    {
        public string OrderId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string KeyId { get; set; } = string.Empty;
    }
}
