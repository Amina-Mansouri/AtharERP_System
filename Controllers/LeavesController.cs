using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class LeavesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly NotificationService _notify;
        private readonly AuditService _audit;
        private readonly IConfiguration _config;

        public LeavesController(AppDbContext context, PermissionService permissionService, NotificationService notify, AuditService audit, IConfiguration config)
        {
            _context = context;
            _permissionService = permissionService;
            _notify = notify;
            _audit = audit;
            _config = config;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var myRequests = await _context.LeaveRequests
                .Where(l => l.UserId == CurrentUserId)
                .OrderByDescending(l => l.RequestedAt)
                .ToListAsync();

            var allowance = _config.GetValue<int?>("Leaves:AnnualAllowanceDays") ?? 30;
            var currentYear = DateTime.UtcNow.Year;
            var usedDays = myRequests
                .Where(l => l.Type == LeaveType.Annual && l.Status == LeaveRequestStatus.Approved && l.StartDate.Year == currentYear)
                .Sum(l => l.DaysCount);

            ViewBag.AnnualAllowance = allowance;
            ViewBag.UsedDays = usedDays;
            ViewBag.RemainingDays = Math.Max(0, allowance - usedDays);

            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Leaves.Manage");
            ViewBag.CanManage = canManage;
            if (canManage)
            {
                ViewBag.PendingRequests = await _context.LeaveRequests
                    .Include(l => l.User)
                    .Where(l => l.Status == LeaveRequestStatus.Pending)
                    .OrderBy(l => l.RequestedAt)
                    .ToListAsync();
            }

            return View(myRequests);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveType type, DateTime startDate, DateTime endDate, string? reason)
        {
            if (endDate < startDate)
            {
                TempData["Error"] = "تاريخ النهاية قبل تاريخ البداية";
                return RedirectToAction("Index");
            }

            var request = new LeaveRequest
            {
                UserId = CurrentUserId,
                Type = type,
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                Reason = reason,
                Status = LeaveRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            _context.LeaveRequests.Add(request);
            await _context.SaveChangesAsync();

            var me = await _context.Users.FindAsync(CurrentUserId);
            var approverIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("HR.Leaves.Manage");
            await _notify.NotifyManyAsync(approverIds, $"طلب إجازة جديد من {me?.FullName} ({request.DaysCount} يوم)", NotificationEventType.LeaveRequestCreated, "/Leaves/Index", requiresAction: true, entityType: "LeaveRequest", entityId: request.Id);

            TempData["Success"] = "تم إرسال طلب الإجازة بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Leaves.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int id, bool approved)
        {
            var request = await _context.LeaveRequests.Include(l => l.User).FirstOrDefaultAsync(l => l.Id == id);
            if (request == null)
                return NotFound();

            if (request.Status != LeaveRequestStatus.Pending)
            {
                TempData["Error"] = "تم اتخاذ قرار بهذا الطلب مسبقاً";
                return RedirectToAction("Index");
            }

            request.Status = approved ? LeaveRequestStatus.Approved : LeaveRequestStatus.Rejected;
            request.DecidedById = CurrentUserId;
            request.DecidedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(LeaveRequest), request.Id.ToString(), approved ? "اعتماد طلب إجازة" : "رفض طلب إجازة");
            await _notify.NotifyAsync(request.UserId, approved ? "تم اعتماد طلب إجازتك" : "تم رفض طلب إجازتك", NotificationEventType.LeaveRequestDecided, "/Leaves/Index", entityType: "LeaveRequest", entityId: request.Id);

            TempData["Success"] = approved ? "تم اعتماد الطلب" : "تم رفض الطلب";
            return RedirectToAction("Index");
        }
    }
}