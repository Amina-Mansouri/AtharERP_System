using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class VendorsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public VendorsController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("Supply.View")]
        public async Task<IActionResult> Index()
        {
            var vendors = await _context.Vendors.OrderBy(v => v.Name).ToListAsync();
            return View(vendors);
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string? contactName, string? phone, string? notes)
        {
            _context.Vendors.Add(new Vendor
            {
                Name = name,
                ContactName = contactName,
                Phone = phone,
                Notes = notes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Create", nameof(Vendor), "", $"إضافة مورّد: {name}");

            TempData["Success"] = "تم إضافة المورّد بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, string? contactName, string? phone, string? notes, bool isActive)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
                return NotFound();

            vendor.Name = name;
            vendor.ContactName = contactName;
            vendor.Phone = phone;
            vendor.Notes = notes;
            vendor.IsActive = isActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث بيانات المورّد بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
                return NotFound();

            var hasItems = await _context.SiteSupplyRequestItems.AnyAsync(i => i.VendorId == id);
            if (hasItems)
            {
                TempData["Error"] = "لا يمكن حذف هذا المورّد لارتباطه بطلبات توريد — عطّليه بدلاً من ذلك";
                return RedirectToAction("Index");
            }

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف المورّد بنجاح";
            return RedirectToAction("Index");
        }
    }
}