using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteRequirementsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly FileUploadService _fileUpload;

        public SiteRequirementsController(AppDbContext context, PermissionService permissionService, FileUploadService fileUpload)
        {
            _context = context;
            _permissionService = permissionService;
            _fileUpload = fileUpload;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Index(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            var requirements = await _context.SiteRequirements
                .Include(r => r.SentBy)
                .Include(r => r.SentByContractor)
                .Where(r => r.SiteId == siteId)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            var unreadFromContractor = requirements.Where(r => r.IsFromContractor && !r.IsRead).ToList();
            foreach (var r in unreadFromContractor)
                r.IsRead = true;
            if (unreadFromContractor.Any())
                await _context.SaveChangesAsync();

            ViewBag.Site = site;
            return View(requirements);
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int siteId, string message, IFormFile? attachment)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            string? attachmentPath = null;
            string? attachmentFileName = null;
            if (attachment != null && attachment.Length > 0)
            {
                var result = await _fileUpload.SaveFileUnrestrictedAsync(attachment, $"sites/{siteId}/requirements");
                if (result.Success)
                {
                    attachmentPath = result.FilePath;
                    attachmentFileName = attachment.FileName;
                }
            }

            _context.SiteRequirements.Add(new SiteRequirement
            {
                SiteId = siteId,
                Message = message,
                AttachmentPath = attachmentPath,
                AttachmentFileName = attachmentFileName,
                IsFromContractor = false,
                SentById = CurrentUserId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إرسال المتطلب للمقاول بنجاح";
            return RedirectToAction("Index", new { siteId });
        }
    }
}