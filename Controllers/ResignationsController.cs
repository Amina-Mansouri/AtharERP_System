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
    public class ResignationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly NotificationService _notify;
        private readonly AuditService _audit;

        public ResignationsController(AppDbContext context, PermissionService permissionService, NotificationService notify, AuditService audit)
        {
            _context = context;
            _permissionService = permissionService;
            _notify = notify;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var myRequests = await _context.ResignationRequests
                .Where(r => r.UserId == CurrentUserId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Resignations.Manage");
            ViewBag.CanManage = canManage;
            if (canManage)
            {
                ViewBag.PendingRequests = await _context.ResignationRequests
                    .Include(r => r.User)
                    .Where(r => r.Status == ResignationStatus.Pending)
                    .OrderBy(r => r.RequestedAt)
                    .ToListAsync();
            }

            bool canFinalApprove = await _permissionService.HasPermissionAsync(User, "HR.Resignations.FinalApprove");
            ViewBag.CanFinalApprove = canFinalApprove;
            if (canFinalApprove)
            {
                ViewBag.PendingFinalRequests = await _context.ResignationRequests
                    .Include(r => r.User)
                    .Where(r => r.Status == ResignationStatus.PendingGeneralManager)
                    .OrderBy(r => r.RequestedAt)
                    .ToListAsync();
            }

            ViewBag.HasPending = myRequests.Any(r => r.Status == ResignationStatus.Pending || r.Status == ResignationStatus.PendingGeneralManager);

            return View(myRequests);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DateTime lastWorkingDay, string? reason)
        {
            var hasPending = await _context.ResignationRequests.AnyAsync(r => r.UserId == CurrentUserId && (r.Status == ResignationStatus.Pending || r.Status == ResignationStatus.PendingGeneralManager));
            if (hasPending)
            {
                TempData["Error"] = "لديك طلب استقالة بانتظار الاعتماد بالفعل";
                return RedirectToAction("Index");
            }

            var request = new ResignationRequest
            {
                UserId = CurrentUserId,
                RequestedLastWorkingDay = lastWorkingDay.Date,
                Reason = reason,
                Status = ResignationStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            _context.ResignationRequests.Add(request);
            await _context.SaveChangesAsync();

            var me = await _context.Users.FindAsync(CurrentUserId);
            var approverIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("HR.Resignations.Manage");
            await _notify.NotifyManyAsync(approverIds, $"طلب استقالة جديد من {me?.FullName} — آخر يوم عمل مقترح {request.RequestedLastWorkingDay:yyyy-MM-dd}", NotificationEventType.ResignationRequestCreated, "/Resignations/Index", requiresAction: true, entityType: "ResignationRequest", entityId: request.Id);

            TempData["Success"] = "تم إرسال طلب الاستقالة بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Resignations.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int id, bool approved)
        {
            var request = await _context.ResignationRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
                return NotFound();

            if (request.Status != ResignationStatus.Pending)
            {
                TempData["Error"] = "تم اتخاذ قرار بهذا الطلب مسبقاً";
                return RedirectToAction("Index");
            }

            request.DecidedById = CurrentUserId;
            request.DecidedAt = DateTime.UtcNow;

            if (!approved)
            {
                request.Status = ResignationStatus.Rejected;
                await _context.SaveChangesAsync();
                await _audit.LogAsync(CurrentUserId, "Update", nameof(ResignationRequest), request.Id.ToString(), "رفض طلب استقالة (الشؤون الإدارية)");
                await _notify.NotifyAsync(request.UserId, "تم رفض طلب استقالتك", NotificationEventType.ResignationRequestDecided, "/Resignations/Index", entityType: "ResignationRequest", entityId: request.Id);
                TempData["Success"] = "تم رفض الطلب";
                return RedirectToAction("Index");
            }

            request.Status = ResignationStatus.PendingGeneralManager;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ResignationRequest), request.Id.ToString(), "اعتماد طلب استقالة (الشؤون الإدارية) — بانتظار المدير العام");
            var gmIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("HR.Resignations.FinalApprove");
            await _notify.NotifyManyAsync(gmIds, $"طلب استقالة من {request.User?.FullName} بانتظار اعتمادك النهائي", NotificationEventType.ResignationRequestCreated, "/Resignations/Index", requiresAction: true, entityType: "ResignationRequest", entityId: request.Id);
            await _notify.NotifyAsync(request.UserId, "تم اعتماد طلب استقالتك من الشؤون الإدارية، بانتظار اعتماد المدير العام", NotificationEventType.ResignationRequestDecided, "/Resignations/Index", entityType: "ResignationRequest", entityId: request.Id);

            TempData["Success"] = "تم اعتماد الطلب من الشؤون الإدارية، بانتظار اعتماد المدير العام";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Resignations.FinalApprove")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalDecide(int id, bool approved)
        {
            var request = await _context.ResignationRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
                return NotFound();

            if (request.Status != ResignationStatus.PendingGeneralManager)
            {
                TempData["Error"] = "هذا الطلب ليس بانتظار الاعتماد النهائي";
                return RedirectToAction("Index");
            }

            request.Status = approved ? ResignationStatus.Approved : ResignationStatus.Rejected;
            request.GmDecidedById = CurrentUserId;
            request.GmDecidedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ResignationRequest), request.Id.ToString(), approved ? "اعتماد نهائي لطلب استقالة (المدير العام)" : "رفض نهائي لطلب استقالة (المدير العام)");
            await _notify.NotifyAsync(request.UserId, approved ? $"تم اعتماد طلب استقالتك نهائياً — سيُعطَّل حسابك تلقائياً في {request.RequestedLastWorkingDay:yyyy-MM-dd}" : "تم رفض طلب استقالتك من المدير العام", NotificationEventType.ResignationRequestDecided, "/Resignations/Index", entityType: "ResignationRequest", entityId: request.Id);

            TempData["Success"] = approved ? "تم الاعتماد النهائي للطلب" : "تم رفض الطلب";
            return RedirectToAction("Index");
        }
    }
}