using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using Shagun.DTOs;
using Shagun.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Shagun.Controlers
{
    [ApiController]
    public class ContributionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly Shagun.Services.RazorpayService _razorpay;

        public ContributionsController(ApplicationDbContext db, Shagun.Services.RazorpayService razorpay)
        {
            _db = db;
            _razorpay = razorpay;
        }

        [HttpPost("/api/gifts/{giftId}/contribute")]
        public async Task<IActionResult> ContributeToGift(int giftId, [FromBody] ContributionCreateDto payload)
        {
            var gift = await _db.GiftItems.FindAsync(giftId);
            if (gift == null) return NotFound(new { detail = "Gift not found" });

            int? inviteeId = null;
            if (!string.IsNullOrEmpty(payload.InviteeToken))
            {
                var invitee = await _db.Invitees.FirstOrDefaultAsync(i => i.InviteToken == payload.InviteeToken);
                if (invitee == null) return BadRequest(new { detail = "Invalid invitation token" });
                inviteeId = invitee.Id;
            }

            var payment = new Payment
            {
                Amount = payload.Amount,
                Currency = "INR",
                Status = "INITIATED",
                Gateway = "RAZORPAY"
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            var contribution = new Contribution
            {
                GiftItemId = giftId,
                Amount = payload.Amount,
                Anonymous = payload.Anonymous,
                Status = "PENDING",
                InviteeId = inviteeId,
                PaymentId = payment.Id
            };

            _db.Contributions.Add(contribution);
            await _db.SaveChangesAsync();

            // Create Razorpay order
            int amountInPaise = (int)(payload.Amount * 100M);
            var orderJson = await _razorpay.CreateOrderAsync(amountInPaise, "INR", receipt: "contrib_" + contribution.Id);
            if (orderJson == null)
            {
                return StatusCode(500, new { detail = "Failed to create Razorpay order" });
            }

            string? orderId = null;
            int? returnedAmount = null;
            string? currency = null;
            try
            {
                if (orderJson.Value.TryGetProperty("id", out var idProp)) orderId = idProp.GetString();
                if (orderJson.Value.TryGetProperty("amount", out var amtProp) && amtProp.TryGetInt32(out var ai)) returnedAmount = ai;
                if (orderJson.Value.TryGetProperty("currency", out var curProp)) currency = curProp.GetString();
            }
            catch { }

            if (string.IsNullOrEmpty(orderId)) return StatusCode(500, new { detail = "Invalid order returned from Razorpay" });

            // Save gateway order id to payment
            payment.GatewayOrderId = orderId;
            await _db.SaveChangesAsync();

            var orderDetails = new { id = orderId, amount = returnedAmount ?? amountInPaise, currency = currency ?? "INR" };

            var contributionDto = new ContributionResponseDto
            {
                Id = contribution.Id,
                GiftItemId = contribution.GiftItemId,
                Amount = contribution.Amount,
                Anonymous = contribution.Anonymous,
                Status = contribution.Status,
                CreatedAt = contribution.CreatedAt
            };

            var orderDto = new Shagun.DTOs.RazorpayOrderResponseDto
            {
                OrderId = orderId,
                Amount = returnedAmount ?? amountInPaise,
                Currency = currency ?? "INR",
                KeyId = _razorpay.KeyId ?? string.Empty
            };

            return Created(string.Empty, new { contribution = contributionDto, razorpay_order = orderDto });
        }

        [HttpGet("/api/events/{eventId}/contributions")]
        public async Task<IActionResult> ListEventContributions(int eventId, [FromQuery] int hostId)
        {
            //var ev = await _db.Events.FindAsync(eventId);
           // if (ev == null || ev.HostId != hostId) return NotFound(new { detail = "Event not found or unauthorized" });

            var contributions = await _db.Contributions
                .Include(c => c.Invitee)
                .Include(c => c.GiftItem)
                .Where(c => c.GiftItem.EventId == eventId)
                .ToListAsync();

            var res = contributions.Select(c => new ContributionResponseDto
            {
                Id = c.Id,
                GiftItemId = c.GiftItemId,
                Amount = c.Amount,
                Anonymous = c.Anonymous,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                InviteeName = c.Invitee != null ? c.Invitee.Name : "Direct Contributor"
            }).ToList();

            return Ok(res);
        }

        [HttpGet("/api/events/{eventId}/public-contributions")]
        public async Task<IActionResult> GetPublicContributionFeed(int eventId)
        {
            var contributions = await _db.Contributions
                .Include(c => c.Invitee)
                .Include(c => c.GiftItem)
                .Where(c => c.GiftItem.EventId == eventId && c.Status == "SUCCESS")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var res = contributions.Select(c => new {
                id = c.Id,
                gift_item_id = c.GiftItemId,
                gift_item_name = c.GiftItem?.Name,
                amount = c.Amount,
                display_name = c.Anonymous ? "Anonymous" : (c.Invitee != null ? c.Invitee.Name : "Well Wisher"),
                created_at = c.CreatedAt
            });

            return Ok(res);
        }
    }
}
