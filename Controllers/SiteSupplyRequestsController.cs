using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteSupplyRequestsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notify;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _audit;

        public SiteSupplyRequestsController(
            AppDbContext context,
            NotificationService notify,
            UserManager<ApplicationUser> userManager,
            AuditService audit)
        {
            _context = context;
            _notify = notify;
            _userManager = userManager;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة طلبات توريد موقع معيّن
        // ============================================
        [RequirePermission("Supply.View")]
        public async Task<IActionResult> Index(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            var requests = await _context.SiteSupplyRequests
                .Include(r => r.RequestedBy)
                .Where(r => r.SiteId == siteId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            ViewBag.Site = site;
            return View(requests);
        }

        // ============================================
        // إنشاء طلب توريد (يرتبط تلقائياً بمشروع الموقع)
        // ============================================
        [RequirePermission("Supply.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("SiteId,MaterialName,Dimensions,Quantity,Unit,Notes")] SiteSupplyRequest model)
        {
            var site = await _context.Sites.FindAsync(model.SiteId);
            if (site == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var errors = string.Join("، ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Error"] = $"بيانات طلب التوريد غير صحيحة: {errors}";
                return RedirectToAction("Index", new { siteId = model.SiteId });
            }

            model.ProjectId = site.ProjectId;
            model.Status = SiteSupplyStatus.Pending;
            model.RequestDate = DateTime.UtcNow;
            model.RequestedById = CurrentUserId;

            _context.SiteSupplyRequests.Add(model);
            await _context.SaveChangesAsync();

            // ملاحظة: لا يوجد حالياً دور/قسم "توريدات" مخصَّص في النظام (نفس فجوة قسم المالية المؤجَّلة)،
            // فيصل الإشعار مؤقتاً لمدير النظام فقط إلى حين استحداث دور مختص لاحقاً
            var adminIds = (await _userManager.GetUsersInRoleAsync("مدير النظام")).Select(u => u.Id);
          

            TempData["Success"] = "تم إرسال طلب التوريد بنجاح";
            return RedirectToAction("Index", new { siteId = model.SiteId });
        }

        // ============================================
        // تحديث حالة طلب التوريد
        // ============================================
        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, SiteSupplyStatus status)
        {
            var request = await _context.SiteSupplyRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            request.Status = status;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(SiteSupplyRequest), id.ToString(), $"تحديث حالة طلب توريد إلى: {status}");

           

            TempData["Success"] = "تم تحديث حالة طلب التوريد";
            return RedirectToAction("Index", new { siteId = request.SiteId });
        }

        // ============================================
        // حذف طلب توريد (تصحيح طلب خاطئ)
        // ============================================
        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.SiteSupplyRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            var siteId = request.SiteId;
            _context.SiteSupplyRequests.Remove(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف طلب التوريد بنجاح";
            return RedirectToAction("Index", new { siteId });
        }
    }
}