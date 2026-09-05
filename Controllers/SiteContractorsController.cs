using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteContractorsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly PermissionService _permissionService;

        public SiteContractorsController(AppDbContext context, AuditService audit, PermissionService permissionService)
        {
            _context = context;
            _audit = audit;
            _permissionService = permissionService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة مقاولي موقع معيّن
        // ============================================
        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Index(int? siteId)
        {
            Site? site = null;
            var query = _context.SiteContractors.Include(c => c.Site).ThenInclude(s => s.Project).AsQueryable();

            if (siteId.HasValue)
            {
                site = await _context.Sites.FindAsync(siteId.Value);
                if (site == null)
                    return NotFound();

                if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                    return Forbid();

                query = query.Where(c => c.SiteId == siteId.Value);
            }
            else if (!await _permissionService.HasPermissionAsync(User, "Projects.ViewAll"))
            {
                var myProjectIds = await _context.ProjectTeamMembers
                    .Where(tm => tm.UserId == CurrentUserId)
                    .Select(tm => tm.ProjectId)
                    .ToListAsync();

                query = query.Where(c => c.Site.Project.CreatedById == CurrentUserId || myProjectIds.Contains(c.Site.ProjectId));
            }

            var contractors = await query
                .OrderByDescending(c => c.Status == ContractorStatus.Active)
                .ThenBy(c => c.Name)
                .ToListAsync();

            ViewBag.Site = site;
            return View(contractors);
        }

        // ============================================
        // إنشاء مقاول
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("SiteId,Name,CompanyName,Phone,Specialty,StartDate,EndDate,Notes")] SiteContractor model)
        {
            var site = await _context.Sites.FindAsync(model.SiteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات المقاول غير صحيحة";
                return RedirectToAction("Index", new { siteId = model.SiteId });
            }

            model.Status = ContractorStatus.Active;
            _context.SiteContractors.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تمت إضافة المقاول {model.Name} بنجاح";
            return RedirectToAction("Index", new { siteId = model.SiteId });
        }

        // ============================================
        // تعديل مقاول
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var contractor = await _context.SiteContractors.Include(c => c.Site).FirstOrDefaultAsync(c => c.Id == id);
            if (contractor == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, contractor.Site.ProjectId))
                return Forbid();

            return View(contractor);
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Name,CompanyName,Phone,Specialty,StartDate,EndDate,Status,Notes")] SiteContractor model)
        {
            var contractor = await _context.SiteContractors.Include(c => c.Site).FirstOrDefaultAsync(c => c.Id == id);
            if (contractor == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, contractor.Site.ProjectId))
                return Forbid();

            if (!ModelState.IsValid)
                return View(model);

            contractor.Name = model.Name;
            contractor.CompanyName = model.CompanyName;
            contractor.Phone = model.Phone;
            contractor.Specialty = model.Specialty;
            contractor.StartDate = model.StartDate;
            contractor.EndDate = model.EndDate;
            contractor.Status = model.Status;
            contractor.Notes = model.Notes;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم تحديث بيانات المقاول {contractor.Name} بنجاح";
            return RedirectToAction("Index", new { siteId = contractor.SiteId });
        }

        // ============================================
        // حذف مقاول
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var contractor = await _context.SiteContractors.Include(c => c.Site).FirstOrDefaultAsync(c => c.Id == id);
            if (contractor == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, contractor.Site.ProjectId))
                return Forbid();

            var siteId = contractor.SiteId;
            var name = contractor.Name;

            _context.SiteContractors.Remove(contractor);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(SiteContractor), id.ToString(), $"حذف مقاول: {name}");

            TempData["Success"] = "تم حذف المقاول بنجاح";
            return RedirectToAction("Index", new { siteId });
        }
    }
}