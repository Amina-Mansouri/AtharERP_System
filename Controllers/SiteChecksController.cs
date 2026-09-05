using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteChecksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly PermissionService _permissionService;

        public SiteChecksController(AppDbContext context, AuditService audit, PermissionService permissionService)
        {
            _context = context;
            _audit = audit;
            _permissionService = permissionService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة فحوصات الجودة والسلامة لموقع معيّن
        // ============================================
        [RequirePermission("Quality.View")]
        public async Task<IActionResult> Index(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            var qualityChecks = await _context.SiteQualityChecks
                              .Include(q => q.CheckedBy)
                .Include(q => q.CheckedByContractor)
                .Include(q => q.ApprovedBy)
                .Where(q => q.SiteId == siteId)
                .OrderByDescending(q => q.CheckDate)
                .ToListAsync();

            var safetyChecks = await _context.SiteSafetyChecks
                               .Include(s => s.CheckedBy)
                .Include(s => s.CheckedByContractor)
                .Where(s => s.SiteId == siteId)
                .OrderByDescending(s => s.CheckDate)
                .ToListAsync();

            ViewBag.Site = site;
            ViewBag.SafetyChecks = safetyChecks;

            return View(qualityChecks);
        }

        // ============================================
        // فحوصات الجودة (SiteQualityCheck)
        // ============================================
        [RequirePermission("Quality.View")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQualityCheck(
            [Bind("SiteId,QualityType,CheckType,Description,Notes")] SiteQualityCheck model)
        {
            var site = await _context.Sites.FindAsync(model.SiteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات فحص الجودة غير صحيحة";
                return RedirectToAction("Index", new { siteId = model.SiteId });
            }

            model.Result = QualityCheckResult.Pending;
            model.CheckDate = DateTime.UtcNow;
            model.CheckedById = CurrentUserId;
            model.IsApproved = false;

            _context.SiteQualityChecks.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة فحص الجودة بنجاح";
            return RedirectToAction("Index", new { siteId = model.SiteId });
        }

        [RequirePermission("Quality.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveQualityCheck(int id, QualityCheckResult result, string? notes)
        {
            var check = await _context.SiteQualityChecks.Include(q => q.Site).FirstOrDefaultAsync(q => q.Id == id);
            if (check == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, check.Site.ProjectId))
                return Forbid();

            check.Result = result;
            check.Notes = notes;
            check.IsApproved = true;
            check.ApprovedAt = DateTime.UtcNow;
            check.ApprovedById = CurrentUserId;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Approve", nameof(SiteQualityCheck), id.ToString(), $"اعتماد فحص جودة بنتيجة: {result}");

            TempData["Success"] = "تم اعتماد فحص الجودة بنجاح";
            return RedirectToAction("Index", new { siteId = check.SiteId });
        }

        // ============================================
        // فحوصات السلامة (SiteSafetyCheck)
        // ============================================
        [RequirePermission("Quality.View")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSafetyCheck(
            [Bind("SiteId,CheckType,Description,Notes")] SiteSafetyCheck model)
        {
            var site = await _context.Sites.FindAsync(model.SiteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات فحص السلامة غير صحيحة";
                return RedirectToAction("Index", new { siteId = model.SiteId });
            }

            model.Result = SafetyResult.Safe;
            model.CheckDate = DateTime.UtcNow;
            model.CheckedById = CurrentUserId;
            model.IsApproved = false;

            _context.SiteSafetyChecks.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة فحص السلامة بنجاح";
            return RedirectToAction("Index", new { siteId = model.SiteId });
        }

        [RequirePermission("Quality.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSafetyCheck(int id, SafetyResult result, string? notes)
        {
            var check = await _context.SiteSafetyChecks.Include(s => s.Site).FirstOrDefaultAsync(s => s.Id == id);
            if (check == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, check.Site.ProjectId))
                return Forbid();

            check.Result = result;
            check.Notes = notes;
            check.IsApproved = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم اعتماد فحص السلامة بنجاح";
            return RedirectToAction("Index", new { siteId = check.SiteId });
        }
    }
}