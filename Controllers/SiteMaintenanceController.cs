using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteMaintenanceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notify;
        private readonly AuditService _audit;

        public SiteMaintenanceController(AppDbContext context, NotificationService notify, AuditService audit)
        {
            _context = context;
            _notify = notify;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة طلبات الصيانة لموقع معيّن
        // ============================================
        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Index(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            var requests = await _context.SiteMaintenances
                .Include(m => m.Responsible)
                .Where(m => m.SiteId == siteId)
                .OrderByDescending(m => m.RequestDate)
                .ToListAsync();

            ViewBag.Site = site;
            ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();

            return View(requests);
        }

        // ============================================
        // إنشاء طلب صيانة
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("SiteId,MaintenanceType,Description,ResponsibleId,Notes")] SiteMaintenance model)
        {
            var site = await _context.Sites.FindAsync(model.SiteId);
            if (site == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات طلب الصيانة غير صحيحة";
                return RedirectToAction("Index", new { siteId = model.SiteId });
            }

            model.Status = MaintenanceStatus.Pending;
            model.RequestDate = DateTime.UtcNow;

            _context.SiteMaintenances.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة طلب الصيانة بنجاح";
            return RedirectToAction("Index", new { siteId = model.SiteId });
        }

        // ============================================
        // تعديل / تحديث حالة طلب صيانة
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var maintenance = await _context.SiteMaintenances.FindAsync(id);
            if (maintenance == null)
                return NotFound();

            ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
            return View(maintenance);
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("MaintenanceType,Description,Status,Cost,ResponsibleId,Notes")] SiteMaintenance model)
        {
            var maintenance = await _context.SiteMaintenances.Include(m => m.Site).FirstOrDefaultAsync(m => m.Id == id);
            if (maintenance == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
                return View(model);
            }

            var wasCompleted = maintenance.Status == MaintenanceStatus.Completed;

            maintenance.MaintenanceType = model.MaintenanceType;
            maintenance.Description = model.Description;
            maintenance.Status = model.Status;
            maintenance.Cost = model.Cost;
            maintenance.ResponsibleId = model.ResponsibleId;
            maintenance.Notes = model.Notes;

            if (!wasCompleted && maintenance.Status == MaintenanceStatus.Completed)
                maintenance.CompletionDate = DateTime.UtcNow;
            else if (maintenance.Status != MaintenanceStatus.Completed)
                maintenance.CompletionDate = null;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(SiteMaintenance), id.ToString(), $"تحديث طلب صيانة: {maintenance.MaintenanceType}");

            TempData["Success"] = "تم تحديث طلب الصيانة بنجاح";
            return RedirectToAction("Index", new { siteId = maintenance.SiteId });
        }

        // ============================================
        // حذف طلب صيانة
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var maintenance = await _context.SiteMaintenances.FindAsync(id);
            if (maintenance == null)
                return NotFound();

            var siteId = maintenance.SiteId;

            _context.SiteMaintenances.Remove(maintenance);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف طلب الصيانة بنجاح";
            return RedirectToAction("Index", new { siteId });
        }
    }
}