using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using Shagun.Models;
using System.Text.Json;

namespace Shagun.Services
{
    public class PaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly RazorpayService _razorpay;

        public PaymentService(ApplicationDbContext db, RazorpayService razorpay)
        {
            _db = db;
            _razorpay = razorpay;
        }

        public async Task<JsonElement?> CreateRazorpayOrderAsync(Payment payment, string giftName)
        {
            // amount is in rupees; convert to paise
            int amountInPaise = (int)(payment.Amount * 100M);
            var order = await _razorpay.CreateOrderAsync(amountInPaise, payment.Currency ?? "INR", receipt: "payment_" + payment.Id);
            if (order == null) return null;

            if (order.Value.TryGetProperty("id", out var idProp)) payment.GatewayOrderId = idProp.GetString();
            await _db.SaveChangesAsync();
            return order;
        }
    }
}
