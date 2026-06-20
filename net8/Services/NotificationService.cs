using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shagun.Services
{
    public class NotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendContributionNotificationAsync(int contributionId)
        {
            _logger.LogInformation("Notify: contribution {id}", contributionId);
            return Task.CompletedTask;
        }
    }
}
