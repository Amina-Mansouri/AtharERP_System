using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    public class SiteDocumentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;

        public SiteDocumentsController(AppDbContext context, PermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Index(int siteId)
        {
            var site = await _context.Sites.FirstOrDefaultAsync(s => s.Id == siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            var documents = await _context.ProjectDocuments
                .Include(d => d.UploadedBy)
                .Where(d => d.ProjectId == site.ProjectId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            ViewBag.Site = site;
            return View(documents);
        }
    }
}