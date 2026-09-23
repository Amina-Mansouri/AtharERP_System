using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AtharERP_System.Services
{
    // مخزن إشعارات داخل النظام + إرسال نسخة بريدية لكل إشعار
    public class NotificationService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IEmailSender _emailSender;

        public NotificationService(AppDbContext context, IMemoryCache cache, IEmailSender emailSender)
        {
            _context = context;
            _cache = cache;
            _emailSender = emailSender;
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
            _cache.Remove($"NavCounters_{userId}");
            await SendEmailCopyAsync(userId, message);
        }

        public async Task NotifyManyAsync(IEnumerable<string> userIds, string message, NotificationEventType eventType, string? link = null, bool requiresAction = false, string? entityType = null, int? entityId = null)
        {
            var distinctIds = userIds.Distinct().ToList();

            foreach (var userId in distinctIds)
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

            foreach (var userId in distinctIds)
            {
                _cache.Remove($"NavCounters_{userId}");
                await SendEmailCopyAsync(userId, message);
            }
        }

        private async Task<bool> IsEventEnabledForUserAsync(string userId, NotificationEventType eventType)
        {
            var setting = await _context.NotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.EventType == eventType);

            return setting?.IsEnabled ?? true;
        }

        private async Task SendEmailCopyAsync(string userId, string message)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.Email))
                return;

            try
            {
                await _emailSender.SendEmailAsync(user.Email, "إشعار جديد من منظومة أثر", message);
            }
            catch
            {
                // فشل إرسال البريد لا يجب أن يوقف إنشاء الإشعار داخل النظام
            }
        }
    }
}