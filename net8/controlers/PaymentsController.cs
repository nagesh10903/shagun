using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using Shagun.DTOs;
using Shagun.Models;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Shagun.Controlers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PaymentsController> _logger;

        private readonly Shagun.Services.Interfaces.IRazorpayService.IRazorpayService _razorpay;
        private readonly IConfiguration _config;

        public PaymentsController(ApplicationDbContext db, ILogger<PaymentsController> logger, Shagun.Services.Interfaces.IRazorpayService.IRazorpayService razorpay, IConfiguration config)
        {
            _db = db;
            _logger = logger;
            _razorpay = razorpay;
            _config = config;
        }

        [HttpGet("/api/payments/razorpay-config")]
        public IActionResult GetRazorpayConfig()
        {
            // Prefer explicit config flag if provided
            var cfgUseMock = _config.GetValue<bool?>("Razorpay:UseMock");
            bool useMock = cfgUseMock ?? string.IsNullOrEmpty(_razorpay.KeyId);

            return Ok(new { use_mock = useMock, key_id = _razorpay.KeyId ?? string.Empty });
        }

        [HttpPost("/api/payments/verify")]
        public async Task<IActionResult> VerifyPayment([FromBody] PaymentVerificationDto payload)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.GatewayOrderId == payload.RazorpayOrderId);
            if (payment == null) return BadRequest(new { detail = "Payment record not found" });

            // Verify signature using RazorpayService
            var valid = _razorpay.VerifyPaymentSignature(payload.RazorpayOrderId, payload.RazorpayPaymentId, payload.RazorpaySignature);
            if (!valid) return BadRequest(new { detail = "Payment signature verification failed" });

            // Update payment with gateway info
            payment.GatewayPaymentId = payload.RazorpayPaymentId;
            payment.GatewaySignature = payload.RazorpaySignature;
            payment.Status = "SUCCESS";
            await _db.SaveChangesAsync();

            // Mark contribution as success
            var contribution = await _db.Contributions.FindAsync(payload.ContributionId);
            if (contribution != null)
            {
                contribution.Status = "SUCCESS";
                // update gift contributed amount
                var gift = await _db.GiftItems.FindAsync(contribution.GiftItemId);
                if (gift != null)
                {
                    gift.ContributedAmount += contribution.Amount;
                    if (gift.ContributedAmount >= gift.EstimatedCost) gift.Status = "FUNDED";
                }
                await _db.SaveChangesAsync();
            }

            return Ok(new { status = "success", message = "Payment verified and contribution completed" });
        }

        [HttpPost("/api/payments/webhook")]
        public async Task<IActionResult> RazorpayWebhook()
        {
            using var reader = new System.IO.StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var signature = Request.Headers["x-razorpay-signature"].ToString();

            if (string.IsNullOrEmpty(signature)) return BadRequest(new { detail = "Signature header missing" });

            try
            {
                // Verify webhook signature using RazorpayService
                var payloadBytes = Encoding.UTF8.GetBytes(body);
                var verified = _razorpay.VerifyWebhookSignature(payloadBytes, signature);
                if (!verified) return BadRequest(new { detail = "Invalid webhook signature" });

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var eventType = root.GetProperty("event").GetString();
                _logger.LogInformation("Received Razorpay Webhook: {eventType}", eventType);

                if (eventType == "payment.captured")
                {
                    var paymentEntity = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
                    var orderId = paymentEntity.GetProperty("order_id").GetString();
                    var paymentId = paymentEntity.GetProperty("id").GetString();

                    var payment = await _db.Payments.FirstOrDefaultAsync(p => p.GatewayOrderId == orderId);
                    if (payment != null)
                    {
                        payment.GatewayPaymentId = paymentId;
                        payment.GatewaySignature = signature;
                        payment.Status = "SUCCESS";
                        await _db.SaveChangesAsync();

                        // mark linked contributions
                        foreach (var c in await _db.Contributions.Where(c => c.PaymentId == payment.Id).ToListAsync())
                        {
                            c.Status = "SUCCESS";
                            var gift = await _db.GiftItems.FindAsync(c.GiftItemId);
                            if (gift != null)
                            {
                                gift.ContributedAmount += c.Amount;
                                if (gift.ContributedAmount >= gift.EstimatedCost) gift.Status = "FUNDED";
                            }
                        }
                        await _db.SaveChangesAsync();
                    }
                }
                else if (eventType == "payment.failed")
                {
                    var paymentEntity = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
                    var orderId = paymentEntity.GetProperty("order_id").GetString();
                    var payment = await _db.Payments.FirstOrDefaultAsync(p => p.GatewayOrderId == orderId);
                    if (payment != null)
                    {
                        payment.Status = "FAILED";
                        await _db.SaveChangesAsync();
                        foreach (var c in await _db.Contributions.Where(c => c.PaymentId == payment.Id).ToListAsync())
                        {
                            c.Status = "FAILED";
                        }
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, new { detail = "Error parsing webhook payload" });
            }

            return Ok(new { status = "ok" });
        }
    }
}
