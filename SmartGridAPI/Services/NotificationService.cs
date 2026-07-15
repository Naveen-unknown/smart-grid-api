using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartGridAPI.Data;
using SmartGridAPI.Models;
using Microsoft.Extensions.DependencyInjection;

namespace SmartGridAPI.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IServiceScopeFactory scopeFactory, ILogger<NotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task SendNotificationAsync(string title, string message, string type = "Info", string targetRole = "Electricity Officer")
        {
            // Simulate sending an SMS
            _logger.LogInformation("===============================================");
            _logger.LogInformation($"[SMS SENT] To: {targetRole}");
            _logger.LogInformation($"[SMS TITLE] {title}");
            _logger.LogInformation($"[SMS BODY] {message}");
            _logger.LogInformation("===============================================");

            // Save to database
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    TargetRole = targetRole,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                context.Notifications.Add(notification);
                await context.SaveChangesAsync();
            }
        }
    }
}
