using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class CustodyController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public CustodyController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("HR.Custody.Manage")]
        public async Task<IActionResult> Index(string? holderId, CustodyStatus? status)
        {
            ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
            ViewBag.HolderId = holderId;
            ViewBag.Status = status;

            var query = _context.Custodies.Include(c => c.Holder).Include(c => c.Department).AsQueryable();
            if (!string.IsNullOrEmpty(holderId))
                query = query.Where(c => c.HolderId == holderId);
            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            var items = await query.OrderByDescending(c => c.HandedDate).ToListAsync();
            return View(items);
        }

        [RequirePermission("HR.Custody.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string code, string itemName, string holderId, int? departmentId, DateTime? handedDate)
        {
            var custody = new Custody
            {
                Code = code,
                ItemName = itemName,
                HolderId = holderId,
                DepartmentId = departmentId,
                HandedDate = (handedDate ?? DateTime.UtcNow).Date,
                Status = CustodyStatus.WithEmployee
            };
            _context.Custodies.Add(custody);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Create", nameof(Custody), custody.Id.ToString(), $"إسناد عهدة \"{itemName}\" لموظف");

            TempData["Success"] = "تم تسجيل العهدة بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Custody.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int id)
        {
            var custody = await _context.Custodies.FindAsync(id);
            if (custody == null)
                return NotFound();

            custody.ReturnedDate = DateTime.UtcNow.Date;
            custody.Status = CustodyStatus.Returned;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(Custody), custody.Id.ToString(), "استرجاع عهدة");

            TempData["Success"] = "تم تسجيل استرجاع العهدة";
            return RedirectToAction("Index");
        }
    }
}