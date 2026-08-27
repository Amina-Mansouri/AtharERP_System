using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SitesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public SitesController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة المواقع
        // ============================================
        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Index(string? search, SiteStatus? status)
        {
            var query = _context.Sites.Include(s => s.Project).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.Name.Contains(search) || s.Project.Name.Contains(search));

            if (status.HasValue)
                query = query.Where(s => s.Status == status.Value);

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CanManage = await _context.RolePermissions.AnyAsync() && await HasManagePermissionAsync();

            var sites = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
            return View(sites);
        }

        // ============================================
        // تفاصيل موقع (يشمل مراحل العمل)
        // ============================================
        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Details(int id)
        {
            var site = await _context.Sites
                .Include(s => s.Project)
                .Include(s => s.Operations)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (site == null)
                return NotFound();

            ViewBag.CanManage = await HasManagePermissionAsync();
            ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            return View(site);
        }

        // ============================================
        // إنشاء موقع
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadProjectsAsync();
            return View();
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description,ProjectId,Address,Latitude,Longitude,Status,StartDate,ExpectedEndDate,ActualEndDate")] Site model)
        {
            if (!ModelState.IsValid)
            {
                await LoadProjectsAsync();
                return View(model);
            }

            model.IsActive = true;
            model.CreatedAt = DateTime.UtcNow;

            _context.Sites.Add(model);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Create", nameof(Site), model.Id.ToString(), $"إنشاء موقع: {model.Name}");

            TempData["Success"] = $"تم إنشاء الموقع {model.Name} بنجاح";
            return RedirectToAction("Details", new { id = model.Id });
        }

        // ============================================
        // تعديل موقع
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var site = await _context.Sites.FindAsync(id);
            if (site == null)
                return NotFound();

            await LoadProjectsAsync();
            return View(site);
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Name,Description,ProjectId,Address,Latitude,Longitude,Status,StartDate,ExpectedEndDate,ActualEndDate,IsActive")] Site model)
        {
            var site = await _context.Sites.FindAsync(id);
            if (site == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadProjectsAsync();
                return View(model);
            }

            site.Name = model.Name;
            site.Description = model.Description;
            site.ProjectId = model.ProjectId;
            site.Address = model.Address;
            site.Latitude = model.Latitude;
            site.Longitude = model.Longitude;
         
            site.Status = model.Status;
            site.StartDate = model.StartDate;
            site.ExpectedEndDate = model.ExpectedEndDate;
            site.ActualEndDate = model.ActualEndDate;
            site.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(Site), site.Id.ToString(), $"تعديل موقع: {site.Name}");

            TempData["Success"] = $"تم تحديث الموقع {site.Name} بنجاح";
            return RedirectToAction("Details", new { id });
        }

        // ============================================
        // حذف موقع (يُمنع إن وُجدت بيانات ميدانية مرتبطة به)
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var site = await _context.Sites
                .Include(s => s.Operations)
                .Include(s => s.DailyReports)
                .Include(s => s.QualityChecks)
                .Include(s => s.SafetyChecks)
                .Include(s => s.Contractors)
                .Include(s => s.MaintenanceRequests)
                .Include(s => s.Documents)
                .Include(s => s.SupplyRequests)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (site == null)
                return NotFound();

            var hasData = site.Operations.Any() || site.DailyReports.Any() || site.QualityChecks.Any()
                || site.SafetyChecks.Any() || site.Contractors.Any() || site.MaintenanceRequests.Any()
                || site.Documents.Any() || site.SupplyRequests.Any();

            if (hasData)
            {
                TempData["Error"] = "لا يمكن حذف الموقع لوجود بيانات ميدانية مسجَّلة عليه (مراحل/تقارير/فحوصات/مقاولين/صيانة/مستندات/طلبات توريد)";
                return RedirectToAction("Details", new { id });
            }

            var siteName = site.Name;
            _context.Sites.Remove(site);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(Site), id.ToString(), $"حذف موقع: {siteName}");

            TempData["Success"] = $"تم حذف الموقع {siteName} بنجاح";
            return RedirectToAction("Index");
        }

        // ============================================
        // مراحل العمل داخل الموقع (SiteOperation)
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOperation(
            [Bind("SiteId,Name,Description,Sequence,PlannedStartDate,PlannedEndDate,ResponsibleId,Notes")] SiteOperation model)
        {
            var site = await _context.Sites.FindAsync(model.SiteId);
            if (site == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات المرحلة غير صحيحة";
                return RedirectToAction("Details", new { id = model.SiteId });
            }

            model.Status = OperationStatus.NotStarted;
            model.CompletionPercentage = 0;

            _context.SiteOperations.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تمت إضافة مرحلة العمل {model.Name} بنجاح";
            return RedirectToAction("Details", new { id = model.SiteId });
        }

        [RequirePermission("Sites.Manage")]
        [HttpGet]
        public async Task<IActionResult> EditOperation(int id)
        {
            var op = await _context.SiteOperations.Include(o => o.Site).FirstOrDefaultAsync(o => o.Id == id);
            if (op == null)
                return NotFound();

            ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            return View(op);
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOperation(
            int id,
            [Bind("Name,Description,Sequence,Status,PlannedStartDate,PlannedEndDate,ActualStartDate,ActualEndDate,CompletionPercentage,ResponsibleId,Notes")] SiteOperation model)
        {
            var op = await _context.SiteOperations.Include(o => o.Site).FirstOrDefaultAsync(o => o.Id == id);
            if (op == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
                return View(model);
            }

            op.Name = model.Name;
            op.Description = model.Description;
            op.Sequence = model.Sequence;
            op.Status = model.Status;
            op.PlannedStartDate = model.PlannedStartDate;
            op.PlannedEndDate = model.PlannedEndDate;
            op.ActualStartDate = model.ActualStartDate;
            op.ActualEndDate = model.ActualEndDate;
            op.CompletionPercentage = model.CompletionPercentage;
            op.ResponsibleId = model.ResponsibleId;
            op.Notes = model.Notes;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم تحديث مرحلة العمل {op.Name} بنجاح";
            return RedirectToAction("Details", new { id = op.SiteId });
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOperation(int id)
        {
            var op = await _context.SiteOperations.FindAsync(id);
            if (op == null)
                return NotFound();

            var siteId = op.SiteId;
            _context.SiteOperations.Remove(op);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف مرحلة العمل بنجاح";
            return RedirectToAction("Details", new { id = siteId });
        }

        private async Task LoadProjectsAsync()
        {
            ViewBag.Projects = await _context.Projects.OrderBy(p => p.Name).ToListAsync();
        }

        private async Task<bool> HasManagePermissionAsync()
        {
            var permissionService = HttpContext.RequestServices.GetRequiredService<PermissionService>();
            return await permissionService.HasPermissionAsync(User, "Sites.Manage");
        }
    }
}