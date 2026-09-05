using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index(string? filter, string? eventType, string? period)
        {
            var baseQuery = _context.Notifications.Where(n => n.UserId == CurrentUserId);

            var unreadCount = await baseQuery.CountAsync(n => !n.IsRead);
            var actionCount = await baseQuery.CountAsync(n => n.RequiresAction && !n.IsRead);

            var query = baseQuery;

            if (filter == "unread")
                query = query.Where(n => !n.IsRead);
            else if (filter == "action")
                query = query.Where(n => n.RequiresAction);

            if (!string.IsNullOrEmpty(eventType) && Enum.TryParse<NotificationEventType>(eventType, out var et))
                query = query.Where(n => n.EventType == et);

            if (period == "today")
                query = query.Where(n => n.CreatedAt.Date == DateTime.UtcNow.Date);
            else if (period == "week")
                query = query.Where(n => n.CreatedAt >= DateTime.UtcNow.AddDays(-7));
            else if (period == "month")
                query = query.Where(n => n.CreatedAt >= DateTime.UtcNow.AddMonths(-1));

            var notifications = await query.OrderByDescending(n => n.CreatedAt).Take(200).ToListAsync();

            var settings = await _context.NotificationSettings
                .Where(s => s.UserId == CurrentUserId)
                .ToListAsync();

            var eventSettings = Enum.GetValues<NotificationEventType>()
                .Select(ev => (EventType: ev, IsEnabled: settings.FirstOrDefault(s => s.EventType == ev)?.IsEnabled ?? true))
                .ToList();

            ViewBag.Filter = filter ?? "all";
            ViewBag.EventTypeFilter = eventType;
            ViewBag.Period = period;
            ViewBag.UnreadCount = unreadCount;
            ViewBag.ActionCount = actionCount;
            ViewBag.EventSettings = eventSettings;

            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id, string? returnFilter, string? returnEventType, string? returnPeriod)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == CurrentUserId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            if (notification?.Link != null)
                return Redirect(notification.Link);

            return RedirectToAction("Index", new { filter = returnFilter, eventType = returnEventType, period = returnPeriod });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead(string? returnFilter, string? returnEventType, string? returnPeriod)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == CurrentUserId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
                n.IsRead = true;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { filter = returnFilter, eventType = returnEventType, period = returnPeriod });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSetting(NotificationEventType eventType, bool isEnabled, string? returnFilter, string? returnEventType, string? returnPeriod)
        {
            var setting = await _context.NotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == CurrentUserId && s.EventType == eventType);

            if (setting == null)
                _context.NotificationSettings.Add(new NotificationSetting { UserId = CurrentUserId, EventType = eventType, IsEnabled = isEnabled });
            else
                setting.IsEnabled = isEnabled;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { filter = returnFilter, eventType = returnEventType, period = returnPeriod });
        }
    }
}