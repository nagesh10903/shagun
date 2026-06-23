using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Shagun.Services.Interfaces.IRazorpayService;

namespace Shagun.Services
{
    public class MockRazorpayService : IRazorpayService
    {
        private readonly IConfiguration _config;
        public string? KeyId => _config["Razorpay:KeyId"] ?? string.Empty;

        public MockRazorpayService(IConfiguration config)
        {
            _config = config;
        }

        public Task<JsonElement?> CreateOrderAsync(int amountInPaise, string currency = "INR", string? receipt = null)
        {
            var fallback = new { id = "mock_" + Guid.NewGuid().ToString("N"), amount = amountInPaise, currency = currency };
            var doc = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(fallback));
            return Task.FromResult<JsonElement?>(doc);
        }

        public bool VerifyPaymentSignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
        {
            // In mock, accept any non-empty signature when keys not configured
            if (string.IsNullOrEmpty(razorpaySignature)) return false;
            return true;
        }

        public bool VerifyWebhookSignature(byte[] payload, string signature, string? secret = null)
        {
            if (string.IsNullOrEmpty(signature)) return false;
            return true;
        }
    }
}
