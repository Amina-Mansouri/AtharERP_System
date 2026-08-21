using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class ClientsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public ClientsController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("PR.Clients")]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Clients
                .Include(c => c.Projects)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Name.Contains(search) ||
                    (c.CompanyName != null && c.CompanyName.Contains(search)) ||
                    (c.Phone != null && c.Phone.Contains(search)));
            }

            ViewBag.Search = search;

            var clients = await query.OrderBy(c => c.Name).ToListAsync();
            return View(clients);
        }

        [RequirePermission("PR.Clients")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [RequirePermission("PR.Clients")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,CompanyName,Phone,Email,Address,TaxNumber,Notes")] Client model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.IsActive = true;
            model.CreatedAt = DateTime.UtcNow;

            _context.Clients.Add(model);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Create", nameof(Client), model.Id.ToString(), $"إنشاء عميل: {model.Name}");

            TempData["Success"] = $"تم إضافة العميل {model.Name} بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("PR.Clients")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound();

            return View(client);
        }

        [RequirePermission("PR.Clients")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Name,CompanyName,Phone,Email,Address,TaxNumber,Notes,IsActive")] Client model)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            client.Name = model.Name;
            client.CompanyName = model.CompanyName;
            client.Phone = model.Phone;
            client.Email = model.Email;
            client.Address = model.Address;
            client.TaxNumber = model.TaxNumber;
            client.Notes = model.Notes;
            client.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(Client), client.Id.ToString(), $"تعديل بيانات العميل: {client.Name}");

            TempData["Success"] = $"تم تحديث بيانات العميل {client.Name} بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("PR.Clients")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _context.Clients.Include(c => c.Projects).FirstOrDefaultAsync(c => c.Id == id);
            if (client == null)
                return NotFound();

            if (client.Projects.Any())
            {
                TempData["Error"] = "لا يمكن حذف العميل لوجود مشاريع مرتبطة به";
                return RedirectToAction("Index");
            }

            var clientName = client.Name;
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(Client), id.ToString(), $"حذف العميل: {clientName}");

            TempData["Success"] = $"تم حذف العميل {clientName} بنجاح";
            return RedirectToAction("Index");
        }
    }
}