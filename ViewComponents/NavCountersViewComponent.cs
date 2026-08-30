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

            // استعلامات متسلسلة (await واحد تلو الآخر) — DbContext واحد لا يدعم التوازي
            var unread = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

            var qualityCount = canApprove
                ? await _context.SiteQualityChecks.CountAsync(q => !q.IsApproved)
                : 0;

            var safetyCount = canApprove
                ? await _context.SiteSafetyChecks.CountAsync(s => !s.IsApproved)
                : 0;

            var supplyCount = canSupplyApprove
                ? await _context.SiteSupplyRequests.CountAsync(r => r.Status == SiteSupplyStatus.Pending)
                : 0;
            var result = new NavCountersViewModel
            {
                UnreadNotifications = unread,
                PendingApprovals = qualityCount + safetyCount,
                MissingDailyReports = 0,
                PendingSupplyRequests = supplyCount
            };

            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(60));
            return View(result);
        }
    }
}