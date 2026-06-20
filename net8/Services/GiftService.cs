using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using Shagun.Models;
using System.Linq;

namespace Shagun.Services
{
    public class GiftService
    {
        private readonly ApplicationDbContext _db;

        public GiftService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(Contribution contribution, Payment payment)> InitiateContributionAsync(int giftId, decimal amount, int? inviteeId, bool anonymous)
        {
            var gift = await _db.GiftItems.FindAsync(giftId);
            if (gift == null) throw new ArgumentException("Gift not found");

            var remaining = gift.EstimatedCost - gift.ContributedAmount;
            if (amount <= 0 || amount > remaining) throw new ArgumentException("Invalid contribution amount");

            var payment = new Payment
            {
                Amount = amount,
                Currency = "INR",
                Status = "INITIATED",
                Gateway = "RAZORPAY"
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            var contribution = new Contribution
            {
                GiftItemId = giftId,
                Amount = amount,
                Anonymous = anonymous,
                Status = "PENDING",
                InviteeId = inviteeId,
                PaymentId = payment.Id
            };
            _db.Contributions.Add(contribution);
            await _db.SaveChangesAsync();

            return (contribution, payment);
        }

        public async Task ProcessSuccessfulPaymentAsync(int paymentId, string gatewayPaymentId, string gatewaySignature)
        {
            var payment = await _db.Payments.FindAsync(paymentId);
            if (payment == null) return;
            payment.GatewayPaymentId = gatewayPaymentId;
            payment.GatewaySignature = gatewaySignature;
            payment.Status = "SUCCESS";
            await _db.SaveChangesAsync();

            var contributions = await _db.Contributions.Where(c => c.PaymentId == paymentId).ToListAsync();
            foreach (var c in contributions)
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

        public async Task ProcessFailedPaymentAsync(int paymentId)
        {
            var payment = await _db.Payments.FindAsync(paymentId);
            if (payment == null) return;
            payment.Status = "FAILED";
            await _db.SaveChangesAsync();

            var contributions = await _db.Contributions.Where(c => c.PaymentId == paymentId).ToListAsync();
            foreach (var c in contributions)
            {
                c.Status = "FAILED";
            }
            await _db.SaveChangesAsync();
        }
    }
}
