using AtharERP_System.Data;
using AtharERP_System.Models.Entities;

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

        public async Task NotifyAsync(string userId, string message, string? link = null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = message,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task NotifyManyAsync(IEnumerable<string> userIds, string message, string? link = null)
        {
            foreach (var userId in userIds.Distinct())
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Message = message,
                    Link = link,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}