using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Text;

namespace Shagun.Controlers
{
    [ApiController]
    [Route("api/events/{eventId}/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly Shagun.Services.CurrentUserService _currentUserService;

        public ReportsController(ApplicationDbContext db, Shagun.Services.CurrentUserService currentUserService)
        {
            _db = db;
            _currentUserService = currentUserService;
        }

        [HttpGet("/api/events/{eventId}/reports/summary")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetEventSummary(int eventId)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null || ev.HostId != currentUser.Id) return NotFound(new { detail = "Event not found or unauthorized" });

            var totalGifts = await _db.GiftItems.CountAsync(g => g.EventId == eventId);
            var fundedGifts = await _db.GiftItems.CountAsync(g => g.EventId == eventId && g.Status == "FUNDED");
            var totalInvitees = await _db.Invitees.CountAsync(i => i.EventId == eventId);

            var sums = await _db.GiftItems.Where(g => g.EventId == eventId).GroupBy(g => 1).Select(g => new {
                target = g.Sum(x => (decimal?)x.EstimatedCost) ?? 0M,
                received = g.Sum(x => (decimal?)x.ContributedAmount) ?? 0M
            }).FirstOrDefaultAsync();

            var targetAmount = (double)(sums?.target ?? 0M);
            var receivedAmount = (double)(sums?.received ?? 0M);
            var remaining = Math.Max(0.0, targetAmount - receivedAmount);
            var fundingPercentage = targetAmount > 0 ? (receivedAmount / targetAmount * 100.0) : 0.0;

            return Ok(new {
                total_gifts = totalGifts,
                funded_gifts = fundedGifts,
                total_invitees = totalInvitees,
                target_amount = targetAmount,
                received_amount = receivedAmount,
                remaining_amount = remaining,
                funding_percentage = Math.Round(fundingPercentage, 1)
            });
        }

        [HttpGet("/api/events/{eventId}/reports/export")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ExportContributions(int eventId)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null || ev.HostId != currentUser.Id) return NotFound(new { detail = "Event not found or unauthorized" });

            var contributions = await _db.Contributions.Include(c => c.GiftItem).Include(c => c.Invitee).Where(c => c.GiftItem.EventId == eventId).ToListAsync();

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            sw.WriteLine("Contribution ID,Contributor Name,Phone Number,Email,Gift Item Name,Amount (₹),Payment ID,Anonymous,Status,Created At");
            foreach (var c in contributions)
            {
                var name = c.Invitee != null ? c.Invitee.Name : "Direct contributor";
                var phone = c.Invitee != null ? c.Invitee.Phone : "N/A";
                var email = c.Invitee != null ? c.Invitee.Email : "N/A";
                var paymentId = c.Payment != null ? c.Payment.GatewayPaymentId : "N/A";
                var anon = c.Anonymous ? "Yes" : "No";
                var created = c.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                sw.WriteLine($"{c.Id},{Escape(name)},{Escape(phone)},{Escape(email)},{Escape(c.GiftItem.Name)},{c.Amount},{paymentId},{anon},{c.Status},{created}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var filename = $"contributions_event_{eventId}.csv";
            return File(bytes, "text/csv", filename);
        }

        private string Escape(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(",") || s.Contains("\"")) return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
