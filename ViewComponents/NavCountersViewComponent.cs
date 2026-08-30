using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace AtharERP_System.ViewComponents
{
    public class NavCountersViewModel
    {
        public int UnreadNotifications { get; set; }
        public int PendingApprovals { get; set; }
        public int MissingDailyReports { get; set; }
        public int PendingSupplyRequests { get; set; }
    }

    public class NavCountersViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly IMemoryCache _cache;

        public NavCountersViewComponent(AppDbContext context, PermissionService permissionService, IMemoryCache cache)
        {
            _context = context;
            _permissionService = permissionService;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return View(new NavCountersViewModel());

            var cacheKey = $"NavCounters_{userId}";
            if (_cache.TryGetValue(cacheKey, out NavCountersViewModel? cached) && cached != null)
                return View(cached);

            var canApprove = await _permissionService.HasPermissionAsync(UserClaimsPrincipal, "Quality.Approve");
            var canSupplyApprove = await _permissionService.HasPermissionAsync(UserClaimsPrincipal, "Supply.Approve");

            // جلب متزامن واحد (Task.WhenAll) لكل العدّادات معاً، ونتيجتها تُخزَّن ككتلة واحدة لكل مستخدم لمدة 60 ثانية
            var unreadTask = _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            var qualityTask = canApprove ? _context.SiteQualityChecks.CountAsync(q => !q.IsApproved) : Task.FromResult(0);
            var safetyTask = canApprove ? _context.SiteSafetyChecks.CountAsync(s => !s.IsApproved) : Task.FromResult(0);
            var supplyTask = canSupplyApprove ? _context.SiteSupplyRequests.CountAsync(r => r.Status == SiteSupplyStatus.Pending) : Task.FromResult(0);

            await Task.WhenAll(unreadTask, qualityTask, safetyTask, supplyTask);

            var result = new NavCountersViewModel
            {
                UnreadNotifications = unreadTask.Result,
                PendingApprovals = qualityTask.Result + safetyTask.Result,
                // TODO(المرحلة ٥): خوارزمية BACKEND.md §9 (فجوة تواريخ التقارير اليومية ناقص الجمعة) — 0 مؤقتاً
                MissingDailyReports = 0,
                PendingSupplyRequests = supplyTask.Result
            };

            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(60));
            return View(result);
        }
    }
}