using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================
        // قائمة المشاريع + البحث والتصفية
        // ============================================
        [RequirePermission("Projects.View")]
        public async Task<IActionResult> Index(string? search, ProjectStatus? status)
        {
            var query = _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.ParentProject)
                .Include(p => p.Client)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Code.Contains(search) ||
                    (p.Location != null && p.Location.Contains(search)));
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;

            var projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(projects);
        }

        // ============================================
        // تفاصيل مشروع (يشمل العميل، المراحل، الخطوات، المهام والتبعيات)
        // ============================================
        [RequirePermission("Projects.View")]
        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.ParentProject)
                .Include(p => p.ChildProjects)
                .Include(p => p.Client)
                .Include(p => p.ProjectEngineers).ThenInclude(pe => pe.User)
                .Include(p => p.Stages).ThenInclude(s => s.AssignedEngineer)
                .Include(p => p.Stages).ThenInclude(s => s.Steps)
                .Include(p => p.Stages).ThenInclude(s => s.Tasks).ThenInclude(t => t.AssignedTo)
                .Include(p => p.Stages).ThenInclude(s => s.Tasks).ThenInclude(t => t.Dependencies).ThenInclude(d => d.DependsOnTask)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            project.Stages = project.Stages.OrderBy(s => s.Order).ToList();

            // بيانات مساعدة لنماذج إضافة المراحل/المهام المضمّنة في الصفحة
            ViewBag.Engineers = await _userManager.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(project);
        }

        // ============================================
        // إنشاء مشروع جديد
        // ============================================
        [RequirePermission("Projects.Create")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View();
        }

        [RequirePermission("Projects.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description,Location,Latitude,Longitude,StartDate,EndDate,Status,Budget,ParentProjectId,ProjectManagerId,ClientId")] Project model,
            string[] assignedEngineerIds)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                ViewBag.SelectedEngineerIds = assignedEngineerIds?.ToList() ?? new List<string>();
                return View(model);
            }

            model.Code = await GenerateProjectCodeAsync();
            model.CreatedAt = DateTime.UtcNow;
            model.IsActive = true;

            _context.Projects.Add(model);
            await _context.SaveChangesAsync();

            if (assignedEngineerIds != null && assignedEngineerIds.Length > 0)
            {
                foreach (var userId in assignedEngineerIds)
                {
                    _context.ProjectEngineers.Add(new ProjectEngineer
                    {
                        ProjectId = model.Id,
                        UserId = userId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"تم إنشاء المشروع {model.Name} برمز {model.Code} بنجاح";
            return RedirectToAction("Index");
        }

        // ============================================
        // تعديل مشروع
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectEngineers)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            await LoadDropdownsAsync(id);
            ViewBag.SelectedEngineerIds = project.ProjectEngineers.Select(pe => pe.UserId).ToList();

            return View(project);
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Name,Description,Location,Latitude,Longitude,StartDate,EndDate,Status,Budget,ParentProjectId,ProjectManagerId,ClientId,IsActive")] Project model,
            string[] assignedEngineerIds)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            if (model.ParentProjectId == id)
            {
                ModelState.AddModelError(string.Empty, "لا يمكن أن يكون المشروع أباً لنفسه");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(id);
                ViewBag.SelectedEngineerIds = assignedEngineerIds?.ToList() ?? new List<string>();
                return View(project);
            }

            project.Name = model.Name;
            project.Description = model.Description;
            project.Location = model.Location;
            project.Latitude = model.Latitude;
            project.Longitude = model.Longitude;
            project.StartDate = model.StartDate;
            project.EndDate = model.EndDate;
            project.Status = model.Status;
            project.Budget = model.Budget;
            project.ParentProjectId = model.ParentProjectId;
            project.ProjectManagerId = model.ProjectManagerId;
            project.ClientId = model.ClientId;
            project.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            var existingLinks = await _context.ProjectEngineers.Where(pe => pe.ProjectId == id).ToListAsync();
            _context.ProjectEngineers.RemoveRange(existingLinks);
            await _context.SaveChangesAsync();

            if (assignedEngineerIds != null && assignedEngineerIds.Length > 0)
            {
                foreach (var userId in assignedEngineerIds)
                {
                    _context.ProjectEngineers.Add(new ProjectEngineer { ProjectId = id, UserId = userId });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"تم تحديث المشروع {project.Name} بنجاح";
            return RedirectToAction("Index");
        }

        // ============================================
        // حذف مشروع
        // ============================================
        [RequirePermission("Projects.Delete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects
                .Include(p => p.ChildProjects)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            if (project.ChildProjects.Any())
            {
                TempData["Error"] = "لا يمكن حذف المشروع لوجود مشاريع فرعية مرتبطة به";
                return RedirectToAction("Index");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم حذف المشروع {project.Name} بنجاح";
            return RedirectToAction("Index");
        }

        // ============================================
        // دوال مساعدة
        // ============================================
        private async Task LoadDropdownsAsync(int? excludeProjectId = null)
        {
            var parentQuery = _context.Projects.Where(p => p.IsActive);
            if (excludeProjectId.HasValue)
                parentQuery = parentQuery.Where(p => p.Id != excludeProjectId.Value);

            ViewBag.ParentProjects = await parentQuery.OrderBy(p => p.Name).ToListAsync();

            ViewBag.Engineers = await _userManager.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.Clients = await _context.Clients
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        private async Task<string> GenerateProjectCodeAsync()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"PRJ-{year}-";

            var existingCodes = await _context.Projects
                .Where(p => p.Code.StartsWith(prefix))
                .Select(p => p.Code)
                .ToListAsync();

            int nextNumber = 1;
            if (existingCodes.Count > 0)
            {
                var lastNumber = existingCodes
                    .Select(c => int.TryParse(c.Substring(prefix.Length), out var n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max();
                nextNumber = lastNumber + 1;
            }

            return $"{prefix}{nextNumber:D3}";
        }
    }
}