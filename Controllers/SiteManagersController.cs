using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteManagersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly PermissionService _permissionService;
        private readonly NotificationService _notify;

        public SiteManagersController(AppDbContext context, AuditService audit, PermissionService permissionService, NotificationService notify)
        {
            _context = context;
            _audit = audit;
            _permissionService = permissionService;
            _notify = notify;
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

            var managers = await _context.SiteManagers
                .Include(m => m.User)
                .Where(m => m.SiteId == siteId)
                .OrderBy(m => m.AssignedAt)
                .ToListAsync();

            ViewBag.Site = site;
            ViewBag.AllStaff = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();

            return View(managers);
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int siteId, string userId, string? notes)
        {
            var site = await _context.Sites.Include(s => s.Project).FirstOrDefaultAsync(s => s.Id == siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            var alreadyLinked = await _context.SiteManagers.AnyAsync(sm => sm.SiteId == siteId && sm.UserId == userId);
            if (alreadyLinked)
            {
                TempData["Error"] = "هذا الموظف مرتبط بهذا الموقع بالفعل";
                return RedirectToAction("Index", new { siteId });
            }

            _context.SiteManagers.Add(new SiteManager
            {
                SiteId = siteId,
                UserId = userId,
                Notes = notes,
                AssignedAt = DateTime.UtcNow
            });

            var alreadyTeamMember = await _context.ProjectTeamMembers.AnyAsync(tm => tm.ProjectId == site.ProjectId && tm.UserId == userId);
            if (!alreadyTeamMember)
            {
                _context.ProjectTeamMembers.Add(new ProjectTeamMember
                {
                    ProjectId = site.ProjectId,
                    UserId = userId,
                    Role = TeamRole.Member,
                    JoinedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Create", nameof(SiteManager), "", "تكليف موظف بإدارة موقع");

            await _notify.NotifyAsync(userId, $"تم تكليفك بإدارة موقع مشروع: {site.Project.Code} - {site.Project.Name}", NotificationEventType.TaskAssigned, $"/Sites/Details/{siteId}", entityType: "Site", entityId: siteId);

            TempData["Success"] = "تم تكليف الموظف بإدارة الموقع بنجاح";
            return RedirectToAction("Index", new { siteId });
        }


        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var manager = await _context.SiteManagers.Include(m => m.Site).FirstOrDefaultAsync(m => m.Id == id);
            if (manager == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, manager.Site.ProjectId))
                return Forbid();

            var siteId = manager.SiteId;
            _context.SiteManagers.Remove(manager);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(SiteManager), id.ToString(), "إلغاء تكليف موظف بإدارة موقع");

            TempData["Success"] = "تم إلغاء تكليف الموظف بإدارة الموقع";
            return RedirectToAction("Index", new { siteId });
        }
    }
}