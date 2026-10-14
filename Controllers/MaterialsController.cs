using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class MaterialsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public MaterialsController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("Supply.View")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _context.MaterialCategories.OrderBy(c => c.NameAr).ToListAsync();
            var items = await _context.MaterialItems.Include(i => i.MaterialCategory).OrderBy(i => i.Name).ToListAsync();
            return View(items);
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string nameAr)
        {
            _context.MaterialCategories.Add(new MaterialCategory { NameAr = nameAr, IsActive = true });
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم إضافة التصنيف بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategory(int id, bool isActive)
        {
            var category = await _context.MaterialCategories.FindAsync(id);
            if (category == null) return NotFound();
            category.IsActive = isActive;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.MaterialCategories.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();
            if (category.Items.Any())
            {
                TempData["Error"] = "لا يمكن حذف التصنيف لوجود أصناف مرتبطة به";
                return RedirectToAction("Index");
            }
            _context.MaterialCategories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حذف التصنيف";
            return RedirectToAction("Index");
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateItem(string name, int materialCategoryId)
        {
            _context.MaterialItems.Add(new MaterialItem { Name = name, MaterialCategoryId = materialCategoryId, IsActive = true, CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم إضافة الصنف بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleItem(int id, bool isActive)
        {
            var item = await _context.MaterialItems.FindAsync(id);
            if (item == null) return NotFound();
            item.IsActive = isActive;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.MaterialItems.FindAsync(id);
            if (item == null) return NotFound();
            var usedInRequests = await _context.SiteSupplyRequestItems.AnyAsync(i => i.MaterialItemId == id);
            if (usedInRequests)
            {
                TempData["Error"] = "لا يمكن حذف هذا الصنف لوجود طلبات توريد مرتبطة به";
                return RedirectToAction("Index");
            }
            _context.MaterialItems.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حذف الصنف";
            return RedirectToAction("Index");
        }
    }
}