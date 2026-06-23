using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Shagun.Services
{
    public class RazorpayService : Shagun.Services.Interfaces.IRazorpayService.IRazorpayService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;
        private readonly string _keyId;
        private readonly string _keySecret;

        public string KeyId => _keyId;

        public RazorpayService(IConfiguration config, IHttpClientFactory httpFactory)
        {
            _config = config;
            _http = httpFactory.CreateClient("razorpay");
            _keyId = _config["Razorpay:KeyId"] ?? _config["Jwt:Key"] ?? string.Empty;
            _keySecret = _config["Razorpay:KeySecret"] ?? string.Empty;

            if (!string.IsNullOrEmpty(_keyId) && !string.IsNullOrEmpty(_keySecret))
            {
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
                _http.BaseAddress = new Uri("https://api.razorpay.com/v1/");
            }
        }

        public async Task<JsonElement?> CreateOrderAsync(int amountInPaise, string currency = "INR", string? receipt = null)
        {
            var payload = new
            {
                amount = amountInPaise,
                currency = currency,
                receipt = receipt,
                payment_capture = 1
            };

        // RaZOR PAY API skip call failed, return a fallback order object

                var fallback = new { id = "local_" + Guid.NewGuid().ToString("N"), amount = amountInPaise, currency = currency };
                return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(fallback));


/*
            try{
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var resp = await _http.PostAsync("orders", content);
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) return null;
                var doc = JsonSerializer.Deserialize<JsonElement>(body);
                return doc;
            }
            catch
            {
                // RaZOR PAY API call failed   

                var fallback = new { id = "local_" + Guid.NewGuid().ToString("N"), amount = amountInPaise, currency = currency };
                return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(fallback));
            }
            
            */
        }

        public bool VerifyPaymentSignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
        {
            // Compute HMAC SHA256 of order_id|payment_id and compare hex digest
            try
            {
                var data = razorpayOrderId + "|" + razorpayPaymentId;
                var key = Encoding.UTF8.GetBytes(_keySecret ?? string.Empty);
                using var hmac = new System.Security.Cryptography.HMACSHA256(key);
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                return SlowEquals(computed, razorpaySignature);
            }
            catch
            {
                return false;
            }
        }

        public bool VerifyWebhookSignature(byte[] payload, string signature, string? secret = null)
        {
            try
            {
                var keySecret = secret ?? _keySecret ?? string.Empty;
                using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(keySecret));
                var hash = hmac.ComputeHash(payload);
                var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                return SlowEquals(computed, signature);
            }
            catch
            {
                return false;
            }
        }

        private static bool SlowEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
