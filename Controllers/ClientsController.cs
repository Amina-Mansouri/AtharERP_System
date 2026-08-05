using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    public class ClientsController : Controller
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        [RequirePermission("Clients.View")]
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

        [RequirePermission("Clients.Create")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [RequirePermission("Clients.Create")]
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

            TempData["Success"] = $"تم إضافة العميل {model.Name} بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("Clients.Edit")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound();

            return View(client);
        }

        [RequirePermission("Clients.Edit")]
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

            TempData["Success"] = $"تم تحديث بيانات العميل {client.Name} بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("Clients.Delete")]
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

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم حذف العميل {client.Name} بنجاح";
            return RedirectToAction("Index");
        }
    }
}