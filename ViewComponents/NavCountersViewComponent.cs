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
        public int MyActiveAssignments { get; set; }
        public string Slot { get; set; } = "bell";
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

        public async Task<IViewComponentResult> InvokeAsync(string slot = "bell")
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return View(new NavCountersViewModel { Slot = slot });

            var cacheKey = $"NavCounters_{userId}";
            if (_cache.TryGetValue(cacheKey, out NavCountersViewModel? cached) && cached != null)
            {
                return View(new NavCountersViewModel
                {
                    UnreadNotifications = cached.UnreadNotifications,
                    PendingApprovals = cached.PendingApprovals,
                    MissingDailyReports = cached.MissingDailyReports,
                    PendingSupplyRequests = cached.PendingSupplyRequests,
                    MyActiveAssignments = cached.MyActiveAssignments,
                    Slot = slot
                });
            }

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

            var myActiveAssignments = await _context.AssignmentEngineers.CountAsync(e =>
                e.UserId == userId
                && e.ProjectAssignment.Status != AssignmentStatus.Completed
                && e.ProjectAssignment.Status != AssignmentStatus.Cancelled);

            var result = new NavCountersViewModel
            {
                UnreadNotifications = unread,
                PendingApprovals = qualityCount + safetyCount,
                MissingDailyReports = 0,
                PendingSupplyRequests = supplyCount,
                MyActiveAssignments = myActiveAssignments,
                Slot = slot
            };

            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(60));
            return View(result);
        }
    }
}