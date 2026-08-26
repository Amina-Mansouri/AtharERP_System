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

        public SiteContractorsController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة مقاولي موقع معيّن
        // ============================================
        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Index(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            var contractors = await _context.SiteContractors
                .Where(c => c.SiteId == siteId)
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
            var contractor = await _context.SiteContractors.FindAsync(id);
            if (contractor == null)
                return NotFound();

            return View(contractor);
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Name,CompanyName,Phone,Specialty,StartDate,EndDate,Status,Notes")] SiteContractor model)
        {
            var contractor = await _context.SiteContractors.FindAsync(id);
            if (contractor == null)
                return NotFound();

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
            var contractor = await _context.SiteContractors.FindAsync(id);
            if (contractor == null)
                return NotFound();

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