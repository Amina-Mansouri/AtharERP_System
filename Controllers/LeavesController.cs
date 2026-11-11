using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
        private readonly FileUploadService _fileUpload;
        private readonly HRFormsPdfService _pdfService;
        private readonly IWebHostEnvironment _environment;

        public LeavesController(AppDbContext context, PermissionService permissionService, NotificationService notify, AuditService audit, IConfiguration config, FileUploadService fileUpload, HRFormsPdfService pdfService, IWebHostEnvironment environment)
        {
            _context = context;
            _permissionService = permissionService;
            _notify = notify;
            _audit = audit;
            _config = config;
            _fileUpload = fileUpload;
            _pdfService = pdfService;
            _environment = environment;
        }

        private async Task ArchivePdfAsync(int requestId)
        {
            var full = await _context.LeaveRequests
                .Include(r => r.User).ThenInclude(u => u!.PositionRef)
                .Include(r => r.User).ThenInclude(u => u!.Department)
                .Include(r => r.DecidedBy).ThenInclude(u => u!.PositionRef)
                .Include(r => r.GmDecidedBy).ThenInclude(u => u!.PositionRef)
                .FirstAsync(r => r.Id == requestId);

            var bytes = _pdfService.GenerateLeavePdf(full);
            if (!string.IsNullOrEmpty(full.PdfFilePath))
                _fileUpload.DeleteFile(full.PdfFilePath);

            var result = await _fileUpload.SaveGeneratedFileAsync(bytes, $"leaves/{full.Id}", ".pdf");
            if (result.Success)
            {
                full.PdfFilePath = result.FilePath;
                await _context.SaveChangesAsync();
            }
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

            bool canFinalApprove = await _permissionService.HasPermissionAsync(User, "HR.Leaves.FinalApprove");
            ViewBag.CanFinalApprove = canFinalApprove;
            if (canFinalApprove)
            {
                ViewBag.PendingFinalRequests = await _context.LeaveRequests
                    .Include(l => l.User)
                    .Where(l => l.Status == LeaveRequestStatus.PendingGeneralManager)
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
            await ArchivePdfAsync(request.Id);

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

            request.DecidedById = CurrentUserId;
            request.DecidedAt = DateTime.UtcNow;

            if (!approved)
            {
                request.Status = LeaveRequestStatus.Rejected;
                await _context.SaveChangesAsync();
                await ArchivePdfAsync(request.Id);
                await _audit.LogAsync(CurrentUserId, "Update", nameof(LeaveRequest), request.Id.ToString(), "رفض طلب إجازة (الشؤون الإدارية)");
                await _notify.NotifyAsync(request.UserId, "تم رفض طلب إجازتك", NotificationEventType.LeaveRequestDecided, "/Leaves/Index", entityType: "LeaveRequest", entityId: request.Id);
                TempData["Success"] = "تم رفض الطلب";
                return RedirectToAction("Index");
            }

            request.Status = LeaveRequestStatus.PendingGeneralManager;
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(LeaveRequest), request.Id.ToString(), "اعتماد طلب إجازة (الشؤون الإدارية) — بانتظار المدير العام");
            var gmIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("HR.Leaves.FinalApprove");
            await _notify.NotifyManyAsync(gmIds, $"طلب إجازة من {request.User?.FullName} بانتظار اعتمادك النهائي", NotificationEventType.LeaveRequestCreated, "/Leaves/Index", requiresAction: true, entityType: "LeaveRequest", entityId: request.Id);
            await _notify.NotifyAsync(request.UserId, "تم اعتماد طلب إجازتك من الشؤون الإدارية، بانتظار اعتماد المدير العام", NotificationEventType.LeaveRequestDecided, "/Leaves/Index", entityType: "LeaveRequest", entityId: request.Id);

            TempData["Success"] = "تم اعتماد الطلب من الشؤون الإدارية، بانتظار اعتماد المدير العام";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Leaves.FinalApprove")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalDecide(int id, bool approved)
        {
            var request = await _context.LeaveRequests.Include(l => l.User).FirstOrDefaultAsync(l => l.Id == id);
            if (request == null)
                return NotFound();

            if (request.Status != LeaveRequestStatus.PendingGeneralManager)
            {
                TempData["Error"] = "هذا الطلب ليس بانتظار الاعتماد النهائي";
                return RedirectToAction("Index");
            }

            request.Status = approved ? LeaveRequestStatus.Approved : LeaveRequestStatus.Rejected;
            request.GmDecidedById = CurrentUserId;
            request.GmDecidedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(LeaveRequest), request.Id.ToString(), approved ? "اعتماد نهائي لطلب إجازة (المدير العام)" : "رفض نهائي لطلب إجازة (المدير العام)");
            await _notify.NotifyAsync(request.UserId, approved ? "تم اعتماد طلب إجازتك نهائياً" : "تم رفض طلب إجازتك من المدير العام", NotificationEventType.LeaveRequestDecided, "/Leaves/Index", entityType: "LeaveRequest", entityId: request.Id);

            TempData["Success"] = approved ? "تم الاعتماد النهائي للطلب" : "تم رفض الطلب";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var request = await _context.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id);
            if (request == null || string.IsNullOrEmpty(request.PdfFilePath))
                return NotFound();

            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Leaves.Manage");
            bool canFinalApprove = await _permissionService.HasPermissionAsync(User, "HR.Leaves.FinalApprove");
            if (request.UserId != CurrentUserId && !canManage && !canFinalApprove)
                return Forbid();

            var fullPath = Path.Combine(_environment.WebRootPath, request.PdfFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            return PhysicalFile(fullPath, "application/pdf", $"طلب-اجازة-{request.Id}.pdf");
        }
    }
}