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
    public class ResignationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly NotificationService _notify;
        private readonly AuditService _audit;
        private readonly FileUploadService _fileUpload;
        private readonly HRFormsPdfService _pdfService;
        private readonly IWebHostEnvironment _environment;

        public ResignationsController(AppDbContext context, PermissionService permissionService, NotificationService notify, AuditService audit, FileUploadService fileUpload, HRFormsPdfService pdfService, IWebHostEnvironment environment)
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
            var full = await _context.ResignationRequests
                .Include(r => r.User).ThenInclude(u => u!.PositionRef)
                .Include(r => r.User).ThenInclude(u => u!.Department)
                .Include(r => r.DecidedBy).ThenInclude(u => u!.PositionRef)
                .Include(r => r.GmDecidedBy).ThenInclude(u => u!.PositionRef)
                .Include(r => r.TaskClearances).ThenInclude(c => c.ProjectAssignment).ThenInclude(a => a!.Project)
                .FirstAsync(r => r.Id == requestId);

            var bytes = _pdfService.GenerateResignationPdf(full);
            if (!string.IsNullOrEmpty(full.PdfFilePath))
                _fileUpload.DeleteFile(full.PdfFilePath);

            var result = await _fileUpload.SaveGeneratedFileAsync(bytes, $"resignations/{full.Id}", ".pdf");
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
            var myRequests = await _context.ResignationRequests
                .Where(r => r.UserId == CurrentUserId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Resignations.Manage");
            ViewBag.CanManage = canManage;
            if (canManage)
            {
                var pending = await _context.ResignationRequests
                    .Include(r => r.User)
                    .Where(r => r.Status == ResignationStatus.Pending)
                    .OrderBy(r => r.RequestedAt)
                    .ToListAsync();
                ViewBag.PendingRequests = pending;

                var openTasksByRequest = new Dictionary<int, List<ProjectAssignment>>();
                foreach (var p in pending)
                {
                    openTasksByRequest[p.Id] = await _context.ProjectAssignments
                        .Include(a => a.Project)
                        .Where(a => a.Engineers.Any(e => e.UserId == p.UserId) && a.Status != AssignmentStatus.Completed && a.Status != AssignmentStatus.Cancelled)
                        .ToListAsync();
                }
                ViewBag.OpenTasksByRequest = openTasksByRequest;
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

            if (canManage || canFinalApprove)
            {
                ViewBag.ApprovedRequests = await _context.ResignationRequests
                    .Include(r => r.User)
                    .Where(r => r.Status == ResignationStatus.Approved)
                    .OrderByDescending(r => r.EffectiveLastWorkingDay)
                    .ToListAsync();
            }

            ViewBag.HasPending = myRequests.Any(r => r.Status == ResignationStatus.Pending || r.Status == ResignationStatus.PendingGeneralManager);

            return View(myRequests);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string? reason)
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
                Reason = reason,
                Status = ResignationStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            _context.ResignationRequests.Add(request);
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            var me = await _context.Users.FindAsync(CurrentUserId);
            var approverIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("HR.Resignations.Manage");
            await _notify.NotifyManyAsync(approverIds, $"طلب استقالة جديد من {me?.FullName}", NotificationEventType.ResignationRequestCreated, "/Resignations/Index", requiresAction: true, entityType: "ResignationRequest", entityId: request.Id);

            TempData["Success"] = "تم إرسال طلب الاستقالة بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Resignations.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int id, bool approved, List<int>? assignmentIds, List<DateTime>? taskEndDates)
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
                await ArchivePdfAsync(request.Id);
                await _audit.LogAsync(CurrentUserId, "Update", nameof(ResignationRequest), request.Id.ToString(), "رفض طلب استقالة (الشؤون الإدارية)");
                await _notify.NotifyAsync(request.UserId, "تم رفض طلب استقالتك", NotificationEventType.ResignationRequestDecided, "/Resignations/Index", entityType: "ResignationRequest", entityId: request.Id);
                TempData["Success"] = "تم رفض الطلب";
                return RedirectToAction("Index");
            }

            var effectiveDate = DateTime.UtcNow.Date;
            if (assignmentIds != null && taskEndDates != null && assignmentIds.Count == taskEndDates.Count && assignmentIds.Count > 0)
            {
                for (int i = 0; i < assignmentIds.Count; i++)
                {
                    _context.ResignationTaskClearances.Add(new ResignationTaskClearance
                    {
                        ResignationRequestId = request.Id,
                        ProjectAssignmentId = assignmentIds[i],
                        EndDate = taskEndDates[i].Date
                    });
                }
                effectiveDate = taskEndDates.Max().Date;
            }

            request.EffectiveLastWorkingDay = effectiveDate;
            request.Status = ResignationStatus.PendingGeneralManager;
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ResignationRequest), request.Id.ToString(), $"اعتماد طلب استقالة (الشؤون الإدارية) — بانتظار المدير العام، تاريخ الإيقاف المتوقع {effectiveDate:yyyy-MM-dd}");
            var gmIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("HR.Resignations.FinalApprove");
            await _notify.NotifyManyAsync(gmIds, $"طلب استقالة من {request.User?.FullName} بانتظار اعتمادك النهائي — تاريخ الإيقاف المتوقع {effectiveDate:yyyy-MM-dd}", NotificationEventType.ResignationRequestCreated, "/Resignations/Index", requiresAction: true, entityType: "ResignationRequest", entityId: request.Id);
            await _notify.NotifyAsync(request.UserId, $"تم اعتماد طلب استقالتك من الشؤون الإدارية — تاريخ إيقاف العمل المتوقع {effectiveDate:yyyy-MM-dd}، بانتظار اعتماد المدير العام", NotificationEventType.ResignationRequestDecided, "/Resignations/Index", entityType: "ResignationRequest", entityId: request.Id);

            TempData["Success"] = $"تم اعتماد الطلب — تاريخ إيقاف العمل المتوقع {effectiveDate:yyyy-MM-dd}، بانتظار اعتماد المدير العام";
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
            await ArchivePdfAsync(request.Id);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ResignationRequest), request.Id.ToString(), approved ? "اعتماد نهائي لطلب استقالة (المدير العام)" : "رفض نهائي لطلب استقالة (المدير العام)");
            await _notify.NotifyAsync(request.UserId, approved ? $"تم اعتماد طلب استقالتك نهائياً — سيُعطَّل حسابك تلقائياً في {request.EffectiveLastWorkingDay:yyyy-MM-dd}" : "تم رفض طلب استقالتك من المدير العام", NotificationEventType.ResignationRequestDecided, "/Resignations/Index", entityType: "ResignationRequest", entityId: request.Id);

            TempData["Success"] = approved ? "تم الاعتماد النهائي للطلب" : "تم رفض الطلب";
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustEffectiveDate(int id, DateTime newDate)
        {
            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Resignations.Manage");
            bool canFinalApprove = await _permissionService.HasPermissionAsync(User, "HR.Resignations.FinalApprove");
            if (!canManage && !canFinalApprove)
                return Forbid();

            var request = await _context.ResignationRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null || request.Status != ResignationStatus.Approved)
                return NotFound();

            request.EffectiveLastWorkingDay = newDate.Date;
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            if (request.User != null && request.User.IsSuspended && request.User.SuspendedReason == "استقالة" && newDate.Date > DateTime.UtcNow.Date)
            {
                request.User.IsSuspended = false;
                request.User.IsActive = true;
                request.User.SuspendedReason = null;
                await _context.SaveChangesAsync();
                await _audit.LogAsync(CurrentUserId, "Update", nameof(ApplicationUser), request.User.Id, "إعادة تفعيل الحساب بعد تعديل تاريخ إيقاف الاستقالة");
                await _notify.NotifyAsync(request.UserId, $"تم تفعيل حسابك مجدداً — تاريخ إيقاف العمل الجديد {newDate:yyyy-MM-dd}", NotificationEventType.ResignationRequestDecided, "/Resignations/Index", entityType: "ResignationRequest", entityId: request.Id);
            }
            else
            {
                await _notify.NotifyAsync(request.UserId, $"تم تعديل تاريخ إيقاف العمل إلى {newDate:yyyy-MM-dd}", NotificationEventType.ResignationRequestDecided, "/Resignations/Index", entityType: "ResignationRequest", entityId: request.Id);
            }

            TempData["Success"] = "تم تعديل التاريخ";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var request = await _context.ResignationRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null || string.IsNullOrEmpty(request.PdfFilePath))
                return NotFound();

            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Resignations.Manage");
            bool canFinalApprove = await _permissionService.HasPermissionAsync(User, "HR.Resignations.FinalApprove");
            if (request.UserId != CurrentUserId && !canManage && !canFinalApprove)
                return Forbid();

            var fullPath = Path.Combine(_environment.WebRootPath, request.PdfFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            return PhysicalFile(fullPath, "application/pdf", $"طلب-استقالة-{request.Id}.pdf");
        }
    }
}