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
    public class ComplaintsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly NotificationService _notify;
        private readonly AuditService _audit;
        private readonly FileUploadService _fileUpload;
        private readonly HRFormsPdfService _pdfService;
        private readonly IWebHostEnvironment _environment;

        public ComplaintsController(AppDbContext context, PermissionService permissionService, NotificationService notify, AuditService audit, FileUploadService fileUpload, HRFormsPdfService pdfService, IWebHostEnvironment environment)
        {
            _context = context;
            _permissionService = permissionService;
            _notify = notify;
            _audit = audit;
            _fileUpload = fileUpload;
            _pdfService = pdfService;
            _environment = environment;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task ArchivePdfAsync(int requestId)
        {
            var full = await _context.ComplaintRequests
                .Include(r => r.User).ThenInclude(u => u!.PositionRef)
                .Include(r => r.User).ThenInclude(u => u!.Department)
                .Include(r => r.ResolvedBy).ThenInclude(u => u!.PositionRef)
                .FirstAsync(r => r.Id == requestId);

            var bytes = _pdfService.GenerateComplaintPdf(full);
            if (!string.IsNullOrEmpty(full.PdfFilePath))
                _fileUpload.DeleteFile(full.PdfFilePath);

            var result = await _fileUpload.SaveGeneratedFileAsync(bytes, $"complaints/{full.Id}", ".pdf");
            if (result.Success)
            {
                full.PdfFilePath = result.FilePath;
                await _context.SaveChangesAsync();
            }
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var myRequests = await _context.ComplaintRequests
                .Where(c => c.UserId == CurrentUserId)
                .OrderByDescending(c => c.RequestedAt)
                .ToListAsync();

            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Complaints.Manage");
            ViewBag.CanManage = canManage;
            if (canManage)
            {
                ViewBag.PendingRequests = await _context.ComplaintRequests
                    .Include(c => c.User)
                    .Where(c => c.Status == ComplaintStatus.Pending)
                    .OrderBy(c => c.RequestedAt)
                    .ToListAsync();
            }

            return View(myRequests);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ComplaintType type, string subject, string description)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(description))
            {
                TempData["Error"] = "الموضوع والتفاصيل مطلوبان";
                return RedirectToAction("Index");
            }

            var request = new ComplaintRequest
            {
                UserId = CurrentUserId,
                Type = type,
                Subject = subject,
                Description = description,
                Status = ComplaintStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            _context.ComplaintRequests.Add(request);
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            var me = await _context.Users.FindAsync(CurrentUserId);
            var approverIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("HR.Complaints.Manage");
            await _notify.NotifyManyAsync(approverIds, $"شكوى جديدة من {me?.FullName}: {subject}", NotificationEventType.ComplaintCreated, "/Complaints/Index", requiresAction: true, entityType: "ComplaintRequest", entityId: request.Id);

            TempData["Success"] = "تم إرسال الشكوى بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Complaints.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id, string resolutionNotes)
        {
            if (string.IsNullOrWhiteSpace(resolutionNotes))
            {
                TempData["Error"] = "نص الرد مطلوب";
                return RedirectToAction("Index");
            }

            var request = await _context.ComplaintRequests.FirstOrDefaultAsync(c => c.Id == id);
            if (request == null)
                return NotFound();

            if (request.Status != ComplaintStatus.Pending)
            {
                TempData["Error"] = "تم الرد على هذه الشكوى مسبقاً";
                return RedirectToAction("Index");
            }

            request.Status = ComplaintStatus.Resolved;
            request.ResolvedById = CurrentUserId;
            request.ResolvedAt = DateTime.UtcNow;
            request.ResolutionNotes = resolutionNotes;
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ComplaintRequest), request.Id.ToString(), "رد على شكوى");
            await _notify.NotifyAsync(request.UserId, "تم الرد على شكواك من الشؤون الإدارية", NotificationEventType.ComplaintResolved, "/Complaints/Index", entityType: "ComplaintRequest", entityId: request.Id);

            TempData["Success"] = "تم تسجيل الرد";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var request = await _context.ComplaintRequests.FirstOrDefaultAsync(c => c.Id == id);
            if (request == null || string.IsNullOrEmpty(request.PdfFilePath))
                return NotFound();

            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Complaints.Manage");
            if (request.UserId != CurrentUserId && !canManage)
                return Forbid();

            var fullPath = Path.Combine(_environment.WebRootPath, request.PdfFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            return PhysicalFile(fullPath, "application/pdf", $"شكوى-{request.Id}.pdf");
        }
    }
}