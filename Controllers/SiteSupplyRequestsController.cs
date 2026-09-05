using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteSupplyRequestsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly PermissionService _permissionService;

        public SiteSupplyRequestsController(AppDbContext context, AuditService audit, PermissionService permissionService)
        {
            _context = context;
            _audit = audit;
            _permissionService = permissionService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("Supply.View")]
        public async Task<IActionResult> Index(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            var requests = await _context.SiteSupplyRequests
                              .Include(r => r.RequestedBy)
                .Include(r => r.RequestedByContractor)
                .Where(r => r.SiteId == siteId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            ViewBag.Site = site;
            return View(requests);
        }

        [RequirePermission("Supply.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("SiteId,MaterialName,Dimensions,Quantity,Unit,Notes")] SiteSupplyRequest model)
        {
            var site = await _context.Sites.FindAsync(model.SiteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

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

            TempData["Success"] = "تم إرسال طلب التوريد بنجاح";
            return RedirectToAction("Index", new { siteId = model.SiteId });
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, SiteSupplyStatus status)
        {
            var request = await _context.SiteSupplyRequests.Include(r => r.Site).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, request.Site.ProjectId))
                return Forbid();

            request.Status = status;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(SiteSupplyRequest), id.ToString(), $"تحديث حالة طلب توريد إلى: {status}");

            TempData["Success"] = "تم تحديث حالة طلب التوريد";
            return RedirectToAction("Index", new { siteId = request.SiteId });
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.SiteSupplyRequests.Include(r => r.Site).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, request.Site.ProjectId))
                return Forbid();

            var siteId = request.SiteId;
            _context.SiteSupplyRequests.Remove(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف طلب التوريد بنجاح";
            return RedirectToAction("Index", new { siteId });
        }
    }
}