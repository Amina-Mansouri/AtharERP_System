using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    // مخزن إشعارات أساسي داخل النظام فقط، بدون بريد أو دفع لحظي
    public class NotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task NotifyAsync(string userId, string message, NotificationEventType eventType, string? link = null, bool requiresAction = false, string? entityType = null, int? entityId = null)
        {
            if (!await IsEventEnabledForUserAsync(userId, eventType))
                return;

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = message,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                EventType = eventType,
                SourceModule = "02",
                RequiresAction = requiresAction,
                EntityType = entityType,
                EntityId = entityId
            });

            await _context.SaveChangesAsync();
        }

        public async Task NotifyManyAsync(IEnumerable<string> userIds, string message, NotificationEventType eventType, string? link = null, bool requiresAction = false, string? entityType = null, int? entityId = null)
        {
            foreach (var userId in userIds.Distinct())
            {
                if (!await IsEventEnabledForUserAsync(userId, eventType))
                    continue;

                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Message = message,
                    Link = link,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    EventType = eventType,
                    SourceModule = "02",
                    RequiresAction = requiresAction,
                    EntityType = entityType,
                    EntityId = entityId
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> IsEventEnabledForUserAsync(string userId, NotificationEventType eventType)
        {
            var setting = await _context.NotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.EventType == eventType);

            return setting?.IsEnabled ?? true;
        }
    }
}