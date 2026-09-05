using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteDailyReportsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FileUploadService _fileUpload;
        private readonly AuditService _audit;
        private readonly PermissionService _permissionService;

        public SiteDailyReportsController(AppDbContext context, FileUploadService fileUpload, AuditService audit, PermissionService permissionService)
        {
            _context = context;
            _fileUpload = fileUpload;
            _audit = audit;
            _permissionService = permissionService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة التقارير اليومية لموقع معيّن (مع فلترة بالتاريخ)
        // ============================================
        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Index(int siteId, DateTime? fromDate, DateTime? toDate)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            var query = _context.SiteDailyReports
                .Include(r => r.CreatedBy)
                .Include(r => r.Photos)
                .Where(r => r.SiteId == siteId);

            if (fromDate.HasValue)
                query = query.Where(r => r.ReportDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(r => r.ReportDate <= toDate.Value);

            var reports = await query.OrderByDescending(r => r.ReportDate).ToListAsync();

            ViewBag.Site = site;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(reports);
        }

        // ============================================
        // تفاصيل تقرير يومي (يشمل الصور)
        // ============================================
        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Details(int id)
        {
            var report = await _context.SiteDailyReports
                .Include(r => r.Site)
                .Include(r => r.CreatedBy)
                .Include(r => r.Photos)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, report.Site.ProjectId))
                return Forbid();

            return View(report);
        }

        // ============================================
        // إنشاء تقرير يومي (مع رفع صور اختيارية مباشرة)
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpGet]
        public async Task<IActionResult> Create(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            ViewBag.Site = site;
            return View();
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("SiteId,ReportDate,Weather,WorkersCount,WorkCompleted,Issues,MaterialsUsed,EquipmentUsed,Visits,Notes")] SiteDailyReport model,
            List<IFormFile>? photos)
        {
            var site = await _context.Sites.FindAsync(model.SiteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.Site = site;
                return View(model);
            }

            model.CreatedById = CurrentUserId;
            model.CreatedAt = DateTime.UtcNow;

            _context.SiteDailyReports.Add(model);
            await _context.SaveChangesAsync();

            if (photos != null)
            {
                foreach (var photo in photos.Where(p => p.Length > 0))
                {
                    var result = await _fileUpload.SaveFileAsync(photo, $"sites/{model.SiteId}/daily-reports/{model.Id}");
                    if (result.Success)
                    {
                        _context.SiteDailyReportPhotos.Add(new SiteDailyReportPhoto
                        {
                            DailyReportId = model.Id,
                            FilePath = result.FilePath!,
                            UploadedAt = DateTime.UtcNow
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            await _audit.LogAsync(CurrentUserId, "Create", nameof(SiteDailyReport), model.Id.ToString(), $"تقرير يومي لموقع: {site.Name} بتاريخ {model.ReportDate:yyyy-MM-dd}");

            TempData["Success"] = "تمت إضافة التقرير اليومي بنجاح";
            return RedirectToAction("Index", new { siteId = model.SiteId });
        }

        // ============================================
        // إضافة صورة لتقرير موجود
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(int reportId, IFormFile file, string? description)
        {
            var report = await _context.SiteDailyReports.Include(r => r.Site).FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, report.Site.ProjectId))
                return Forbid();

            var result = await _fileUpload.SaveFileAsync(file, $"sites/{report.SiteId}/daily-reports/{report.Id}");
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Details", new { id = reportId });
            }

            _context.SiteDailyReportPhotos.Add(new SiteDailyReportPhoto
            {
                DailyReportId = reportId,
                FilePath = result.FilePath!,
                Description = description,
                UploadedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة الصورة بنجاح";
            return RedirectToAction("Details", new { id = reportId });
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(int id, int reportId)
        {
            var report = await _context.SiteDailyReports.Include(r => r.Site).FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, report.Site.ProjectId))
                return Forbid();

            var photo = await _context.SiteDailyReportPhotos.FindAsync(id);
            if (photo != null)
            {
                _fileUpload.DeleteFile(photo.FilePath);
                _context.SiteDailyReportPhotos.Remove(photo);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = reportId });
        }

        // ============================================
        // حذف تقرير يومي (يحذف صوره أولاً من القرص)
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var report = await _context.SiteDailyReports.Include(r => r.Site).Include(r => r.Photos).FirstOrDefaultAsync(r => r.Id == id);
            if (report == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, report.Site.ProjectId))
                return Forbid();

            var siteId = report.SiteId;

            foreach (var photo in report.Photos)
                _fileUpload.DeleteFile(photo.FilePath);

            _context.SiteDailyReports.Remove(report);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(SiteDailyReport), id.ToString(), "حذف تقرير يومي");

            TempData["Success"] = "تم حذف التقرير اليومي بنجاح";
            return RedirectToAction("Index", new { siteId });
        }
    }
}