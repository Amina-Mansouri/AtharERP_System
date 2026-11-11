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
    public class PermitsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly NotificationService _notify;
        private readonly AuditService _audit;
        private readonly FileUploadService _fileUpload;
        private readonly HRFormsPdfService _pdfService;
        private readonly IWebHostEnvironment _environment;

        public PermitsController(AppDbContext context, PermissionService permissionService, NotificationService notify, AuditService audit, FileUploadService fileUpload, HRFormsPdfService pdfService, IWebHostEnvironment environment)
        {
            _context = context;
            _permissionService = permissionService;
            _notify = notify;
            _audit = audit;
            _fileUpload = fileUpload;
            _pdfService = pdfService;
            _environment = environment;
        }

        private async Task ArchivePdfAsync(int requestId)
        {
            var full = await _context.PermitRequests
                .Include(r => r.User).ThenInclude(u => u!.PositionRef)
                .Include(r => r.User).ThenInclude(u => u!.Department)
                .Include(r => r.DecidedBy).ThenInclude(u => u!.PositionRef)
                .Include(r => r.GmDecidedBy).ThenInclude(u => u!.PositionRef)
                .FirstAsync(r => r.Id == requestId);

            var bytes = _pdfService.GeneratePermitPdf(full);
            if (!string.IsNullOrEmpty(full.PdfFilePath))
                _fileUpload.DeleteFile(full.PdfFilePath);

            var result = await _fileUpload.SaveGeneratedFileAsync(bytes, $"permits/{full.Id}", ".pdf");
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
            var myRequests = await _context.PermitRequests
                .Where(p => p.UserId == CurrentUserId)
                .OrderByDescending(p => p.RequestedAt)
                .ToListAsync();

            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Permits.Manage");
            ViewBag.CanManage = canManage;
            if (canManage)
            {
                ViewBag.PendingRequests = await _context.PermitRequests
                    .Include(p => p.User)
                    .Where(p => p.Status == PermitStatus.Pending)
                    .OrderBy(p => p.RequestedAt)
                    .ToListAsync();
            }

            bool canFinalApprove = await _permissionService.HasPermissionAsync(User, "HR.Permits.FinalApprove");
            ViewBag.CanFinalApprove = canFinalApprove;
            if (canFinalApprove)
            {
                ViewBag.PendingFinalRequests = await _context.PermitRequests
                    .Include(p => p.User)
                    .Where(p => p.Status == PermitStatus.PendingGeneralManager)
                    .OrderBy(p => p.RequestedAt)
                    .ToListAsync();
            }

            return View(myRequests);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PermitType type, DateTime date, TimeSpan startTime, TimeSpan endTime, string? reason)
        {
            if (endTime <= startTime)
            {
                TempData["Error"] = "وقت النهاية يجب أن يكون بعد وقت البداية";
                return RedirectToAction("Index");
            }

            var request = new PermitRequest
            {
                UserId = CurrentUserId,
                Type = type,
                Date = date.Date,
                StartTime = startTime,
                EndTime = endTime,
                Reason = reason,
                Status = PermitStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            _context.PermitRequests.Add(request);
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            var me = await _context.Users.FindAsync(CurrentUserId);
            var approverIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("HR.Permits.Manage");
            await _notify.NotifyManyAsync(approverIds, $"طلب إذن جديد من {me?.FullName} ({request.DurationHours:N1} ساعة)", NotificationEventType.PermitRequestCreated, "/Permits/Index", requiresAction: true, entityType: "PermitRequest", entityId: request.Id);

            TempData["Success"] = "تم إرسال طلب الإذن بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Permits.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int id, bool approved)
        {
            var request = await _context.PermitRequests.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
            if (request == null)
                return NotFound();

            if (request.Status != PermitStatus.Pending)
            {
                TempData["Error"] = "تم اتخاذ قرار بهذا الطلب مسبقاً";
                return RedirectToAction("Index");
            }

            request.DecidedById = CurrentUserId;
            request.DecidedAt = DateTime.UtcNow;

            if (!approved)
            {
                request.Status = PermitStatus.Rejected;
                await _context.SaveChangesAsync();
                await ArchivePdfAsync(request.Id);
                await _audit.LogAsync(CurrentUserId, "Update", nameof(PermitRequest), request.Id.ToString(), "رفض طلب إذن (الشؤون الإدارية)");
                await _notify.NotifyAsync(request.UserId, "تم رفض طلب إذنك", NotificationEventType.PermitRequestDecided, "/Permits/Index", entityType: "PermitRequest", entityId: request.Id);
                TempData["Success"] = "تم رفض الطلب";
                return RedirectToAction("Index");
            }

            request.Status = PermitStatus.PendingGeneralManager;
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(PermitRequest), request.Id.ToString(), "اعتماد طلب إذن (الشؤون الإدارية) — بانتظار المدير العام");
            var gmIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("HR.Permits.FinalApprove");
            await _notify.NotifyManyAsync(gmIds, $"طلب إذن من {request.User?.FullName} بانتظار اعتمادك النهائي", NotificationEventType.PermitRequestCreated, "/Permits/Index", requiresAction: true, entityType: "PermitRequest", entityId: request.Id);
            await _notify.NotifyAsync(request.UserId, "تم اعتماد طلب إذنك من الشؤون الإدارية، بانتظار اعتماد المدير العام", NotificationEventType.PermitRequestDecided, "/Permits/Index", entityType: "PermitRequest", entityId: request.Id);

            TempData["Success"] = "تم اعتماد الطلب من الشؤون الإدارية، بانتظار اعتماد المدير العام";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Permits.FinalApprove")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalDecide(int id, bool approved)
        {
            var request = await _context.PermitRequests.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
            if (request == null)
                return NotFound();

            if (request.Status != PermitStatus.PendingGeneralManager)
            {
                TempData["Error"] = "هذا الطلب ليس بانتظار الاعتماد النهائي";
                return RedirectToAction("Index");
            }

            request.Status = approved ? PermitStatus.Approved : PermitStatus.Rejected;
            request.GmDecidedById = CurrentUserId;
            request.GmDecidedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(PermitRequest), request.Id.ToString(), approved ? "اعتماد نهائي لطلب إذن (المدير العام)" : "رفض نهائي لطلب إذن (المدير العام)");
            await _notify.NotifyAsync(request.UserId, approved ? "تم اعتماد طلب إذنك نهائياً" : "تم رفض طلب إذنك من المدير العام", NotificationEventType.PermitRequestDecided, "/Permits/Index", entityType: "PermitRequest", entityId: request.Id);
            TempData["Success"] = approved ? "تم الاعتماد النهائي للطلب" : "تم رفض الطلب";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var request = await _context.PermitRequests.FirstOrDefaultAsync(p => p.Id == id);
            if (request == null || string.IsNullOrEmpty(request.PdfFilePath))
                return NotFound();

            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Permits.Manage");
            bool canFinalApprove = await _permissionService.HasPermissionAsync(User, "HR.Permits.FinalApprove");
            if (request.UserId != CurrentUserId && !canManage && !canFinalApprove)
                return Forbid();

            var fullPath = Path.Combine(_environment.WebRootPath, request.PdfFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            return PhysicalFile(fullPath, "application/pdf", $"طلب-اذن-{request.Id}.pdf");
        }
    }
}