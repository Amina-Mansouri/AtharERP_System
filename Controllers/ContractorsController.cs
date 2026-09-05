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
    // إدارة حسابات دخول المقاولين (منفصلة عن هوية موظفي الشركة)
    public class ContractorsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public ContractorsController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("Sites.Manage")]
        public async Task<IActionResult> Index()
        {
            var contractors = await _context.Contractors
                .Include(c => c.SiteAssignments)
                .OrderByDescending(c => c.IsActive)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(contractors);
        }

        [RequirePermission("Sites.Manage")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var contractor = await _context.Contractors.Include(c => c.SiteAssignments).ThenInclude(sa => sa.Site).FirstOrDefaultAsync(c => c.Id == id);
            if (contractor == null)
                return NotFound();

            return View(contractor);
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,CompanyName,Phone,Email,IsActive")] Contractor model)
        {
            var contractor = await _context.Contractors.FindAsync(id);
            if (contractor == null)
                return NotFound();

            if (await _context.Contractors.AnyAsync(c => c.Id != id && c.Email == model.Email))
            {
                ModelState.AddModelError(string.Empty, "البريد الإلكتروني مستخدم بالفعل لمقاول آخر");
            }

            if (!ModelState.IsValid)
                return View(contractor);

            contractor.Name = model.Name;
            contractor.CompanyName = model.CompanyName;
            contractor.Phone = model.Phone;
            contractor.Email = model.Email;
            contractor.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(Contractor), id.ToString(), $"تعديل حساب مقاول: {contractor.Name}");

            TempData["Success"] = "تم تحديث بيانات الحساب بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            var contractor = await _context.Contractors.FindAsync(id);
            if (contractor == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "كلمة المرور يجب ألا تقل عن 6 أحرف";
                return RedirectToAction("Edit", new { id });
            }

            contractor.PasswordHash = new PasswordHasher<Contractor>().HashPassword(contractor, newPassword);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(Contractor), id.ToString(), $"إعادة تعيين كلمة مرور مقاول: {contractor.Name}");

            TempData["Success"] = "تم تحديث كلمة المرور بنجاح";
            return RedirectToAction("Edit", new { id });
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var contractor = await _context.Contractors.Include(c => c.SiteAssignments).FirstOrDefaultAsync(c => c.Id == id);
            if (contractor == null)
                return NotFound();

            if (contractor.SiteAssignments.Any())
            {
                TempData["Error"] = "لا يمكن حذف هذا الحساب لارتباطه بمواقع — عطّليه بدلاً من ذلك، أو أزيلي ارتباطاته أولاً";
                return RedirectToAction("Edit", new { id });
            }

            _context.Contractors.Remove(contractor);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(Contractor), id.ToString(), $"حذف حساب مقاول: {contractor.Name}");

            TempData["Success"] = "تم حذف الحساب بنجاح";
            return RedirectToAction("Index");
        }
    }
}