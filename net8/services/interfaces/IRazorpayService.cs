using System.Text.Json;
using System.Threading.Tasks;

namespace Shagun.Services.Interfaces.IRazorpayService
{
    public interface IRazorpayService
    {
        public string? KeyId { get; }
        public Task<JsonElement?> CreateOrderAsync(int amountInPaise, string currency = "INR", string? receipt = null);
        public bool VerifyPaymentSignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature);
        public bool VerifyWebhookSignature(byte[] payload, string signature, string? secret = null);
    }
}
