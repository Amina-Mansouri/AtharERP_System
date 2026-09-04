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
    public class ProjectsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PermissionService _permissionService;
        private readonly AuditService _audit;

        public ProjectsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            PermissionService permissionService,
            AuditService audit)
        {
            _context = context;
            _userManager = userManager;
            _permissionService = permissionService;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة المشاريع + عزل الرؤية (القسم 6.6.2)
        // ============================================
        [RequirePermission("Projects.ViewOwn", "Projects.ViewAll")]
        public async Task<IActionResult> Index(string? search, ProjectStatus? status, ProjectScope? scope)
        {
            var baseQuery = _context.Projects
                .Include(p => p.Client)
                .Include(p => p.ParentProject)
                .Include(p => p.ChildProjects)
                .AsQueryable();

            baseQuery = await ApplyVisibilityFilterAsync(baseQuery);

            var today = DateTime.UtcNow.Date;
            var soonCutoff = today.AddDays(30);

            ViewBag.TotalProjects = await baseQuery.CountAsync();
            ViewBag.InProgressProjects = await baseQuery.CountAsync(p => p.Status == ProjectStatus.InProgress);
            ViewBag.CompletedProjects = await baseQuery.CountAsync(p => p.Status == ProjectStatus.Completed);
            ViewBag.OnHoldProjects = await baseQuery.CountAsync(p => p.Status == ProjectStatus.OnHold);
            ViewBag.SoonDeliveryProjects = await baseQuery.CountAsync(p => p.Status == ProjectStatus.InProgress && p.PlannedEndDate != null && p.PlannedEndDate >= today && p.PlannedEndDate <= soonCutoff);
            ViewBag.DelayedProjects = await baseQuery.CountAsync(p => p.Status == ProjectStatus.InProgress && p.PlannedEndDate != null && p.PlannedEndDate < today);

            var query = baseQuery;

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Code.Contains(search));
            }

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (scope.HasValue)
                query = query.Where(p => p.Scope == scope.Value);

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Scope = scope;
            ViewBag.CanViewClient = await CanViewClientAsync();

            var projects = await query
    .OrderByDescending(p => p.Priority)
    .ThenByDescending(p => p.CreatedAt)
    .ToListAsync();
            return View(projects);
        }
        // ============================================
        // تفاصيل مشروع (يشمل تجميعاً حياً للمشاريع الفرعية إن كان رئيسياً)
        // ============================================
        [RequirePermission("Projects.ViewOwn", "Projects.ViewAll")]
        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Client)
                .Include(p => p.ParentProject)
                .Include(p => p.ChildProjects)
                .Include(p => p.CreatedBy)
                .Include(p => p.TeamMembers).ThenInclude(tm => tm.User)
                .Include(p => p.Timelines)
                .Include(p => p.Stages).ThenInclude(s => s.AssignedEngineer)
                .Include(p => p.Stages).ThenInclude(s => s.Department)
                .Include(p => p.Stages).ThenInclude(s => s.Steps)
                .Include(p => p.Stages).ThenInclude(s => s.Assignments).ThenInclude(a => a.Engineers).ThenInclude(e => e.User)
                .Include(p => p.Stages).ThenInclude(s => s.Assignments).ThenInclude(a => a.Subtasks)
                .Include(p => p.Tasks).ThenInclude(t => t.Assignees).ThenInclude(a => a.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            if (!await CanAccessProjectAsync(project))
                return Forbid();

            // تجميع حي (بدون تكرار بيانات) لمهام المشاريع الفرعية داخل المشروع الرئيسي (القسم 2.2)
            if (project.Scope == ProjectScope.Main && project.ChildProjects.Any())
            {
                var childIds = project.ChildProjects.Select(c => c.Id).ToList();
                var aggregatedTasks = await _context.ProjectTasks
                    .Include(t => t.Assignees).ThenInclude(a => a.User)
                    .Where(t => childIds.Contains(t.ProjectId))
                    .ToListAsync();

                ViewBag.AggregatedSubProjectTasks = aggregatedTasks;
                ViewBag.AggregatedActualCost = project.ActualCost + await _context.Projects
                    .Where(p => childIds.Contains(p.Id))
                    .SumAsync(p => p.ActualCost);
            }

            ViewBag.CanViewClient = await CanViewClientAsync();
            ViewBag.CanViewCosts = await _permissionService.HasPermissionAsync(User, "Projects.Assignments.View");
            ViewBag.CanEdit = await _permissionService.HasPermissionAsync(User, "Projects.Edit");
            ViewBag.AllEmployees = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
            ViewBag.Engineers = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.StageTemplates = await _context.StageTemplates.Include(t => t.DefaultTasks).OrderBy(t => t.Order).ToListAsync();
            ViewBag.Documents = await _context.ProjectDocuments.Include(d => d.UploadedBy).Where(d => d.ProjectId == id).OrderByDescending(d => d.UploadedAt).ToListAsync();
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(id);
                return View(project);
            }
            return View(project);
        }

        // ============================================
        // إنشاء مشروع
        // ============================================
        [RequirePermission("Projects.Create")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            ViewBag.LastProjectCode = await _context.Projects.OrderByDescending(p => p.Id).Select(p => p.Code).FirstOrDefaultAsync();
            ViewBag.CanEdit = true;
            ViewBag.CanViewCosts = await _permissionService.HasPermissionAsync(User, "Projects.Assignments.View");
            ViewBag.Engineers = new List<ApplicationUser>();
            ViewBag.StageTemplates = await _context.StageTemplates.Include(t => t.DefaultTasks).OrderBy(t => t.Order).ToListAsync();
            ViewBag.Documents = new List<ProjectDocument>();
            return View("Details", new Project());
        }

        [RequirePermission("Projects.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
                     [Bind("Name,Description,ClientId,ParentProjectId,Scope,Type,Code,PlannedStartDate,PlannedEndDate,ActualDeliveryDate,Budget,Priority,AutoTransferToSite")] Project model)
        {
            if (model.PlannedEndDate.HasValue && model.PlannedStartDate.HasValue && model.PlannedEndDate < model.PlannedStartDate)
            {
                ModelState.AddModelError(string.Empty, "تاريخ التسليم المتفق عليه يجب أن يكون بعد تاريخ البدء");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                ViewBag.CanEdit = true;
                ViewBag.StageTemplates = await _context.StageTemplates.Include(t => t.DefaultTasks).OrderBy(t => t.Order).ToListAsync();
                ViewBag.Engineers = new List<ApplicationUser>();
                ViewBag.Documents = new List<ProjectDocument>();
                return View("Details", model);
            }

            if (model.Scope == ProjectScope.Main)
            {
                model.ParentProjectId = null;
            }
            else if (model.ParentProjectId.HasValue)
            {
                var parentForCreate = await _context.Projects.FindAsync(model.ParentProjectId.Value);
                if (parentForCreate != null)
                {
                    model.ClientId = parentForCreate.ClientId;
                    model.Type = parentForCreate.Type;
                }
            }

            model.ActualDeliveryDate = null;

            if (string.IsNullOrWhiteSpace(model.Code))
            {
                model.Code = await GenerateProjectCodeAsync();
            }
            else if (await _context.Projects.AnyAsync(p => p.Code == model.Code))
            {
                ModelState.AddModelError(string.Empty, "رمز المشروع مستخدم بالفعل، اختاري رمزاً آخر أو اتركيه فارغاً للتوليد التلقائي");
                await LoadDropdownsAsync();
                ViewBag.CanEdit = true;
                ViewBag.StageTemplates = await _context.StageTemplates.Include(t => t.DefaultTasks).OrderBy(t => t.Order).ToListAsync();
                ViewBag.Engineers = new List<ApplicationUser>();
                ViewBag.Documents = new List<ProjectDocument>();
                return View("Details", model);
            }

            model.CreatedAt = DateTime.UtcNow;
            model.CreatedById = CurrentUserId;
            model.ActualCost = 0;
            model.CompletionPercentage = 0;

            _context.Projects.Add(model);
            await _context.SaveChangesAsync();

            // منشئ المشروع يُضاف تلقائياً كمدير مشروع في الفريق
            _context.ProjectTeamMembers.Add(new ProjectTeamMember
            {
                ProjectId = model.Id,
                UserId = CurrentUserId,
                Role = TeamRole.ProjectManager,
                JoinedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Create", nameof(Project), model.Id.ToString(), $"إنشاء مشروع: {model.Name} ({model.Code})");

            TempData["Success"] = $"تم إنشاء المشروع {model.Name} برمز {model.Code} بنجاح";
            return RedirectToAction("Details", new { id = model.Id });
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
             [Bind("Name,Description,ClientId,ParentProjectId,Scope,Type,PlannedStartDate,PlannedEndDate,ActualDeliveryDate,Budget,Priority,AutoTransferToSite")] Project model)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            if (model.ParentProjectId == id)
            {
                ModelState.AddModelError(string.Empty, "لا يمكن أن يكون المشروع أباً لنفسه");
            }

            if (model.PlannedEndDate.HasValue && model.PlannedStartDate.HasValue && model.PlannedEndDate < model.PlannedStartDate)
            {
                ModelState.AddModelError(string.Empty, "تاريخ التسليم المتفق عليه يجب أن يكون بعد تاريخ البدء");
            }
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(id);
                model.Id = id;
                model.Code = project.Code;
                ViewBag.CanEdit = true;
                ViewBag.StageTemplates = await _context.StageTemplates.Include(t => t.DefaultTasks).OrderBy(t => t.Order).ToListAsync();
                ViewBag.Engineers = new List<ApplicationUser>();
                ViewBag.Documents = new List<ProjectDocument>();
                return View("Details", model);
            }

            if (model.Scope == ProjectScope.Main)
            {
                model.ParentProjectId = null;
            }
            else if (model.ParentProjectId.HasValue)
            {
                var parent = await _context.Projects.FindAsync(model.ParentProjectId.Value);
                if (parent != null)
                {
                    model.ClientId = parent.ClientId;
                    model.Type = parent.Type;
                }
            }

            if (project.Status != ProjectStatus.Completed)
            {
                model.ActualDeliveryDate = null;
            }

            project.Name = model.Name;
            project.Description = model.Description;
            project.ClientId = model.ClientId;
            project.ParentProjectId = model.ParentProjectId;
            project.Scope = model.Scope;
            project.Type = model.Type;
            project.PlannedStartDate = model.PlannedStartDate;
            project.PlannedEndDate = model.PlannedEndDate;
            project.ActualDeliveryDate = model.ActualDeliveryDate;
            project.Budget = model.Budget;
            project.Priority = model.Priority;
            project.AutoTransferToSite = model.AutoTransferToSite;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(Project), project.Id.ToString(), $"تعديل مشروع: {project.Name}");

            TempData["Success"] = $"تم تحديث المشروع {project.Name} بنجاح";
            return RedirectToAction("Details", new { id });
        }


        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hold(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            project.Status = ProjectStatus.OnHold;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Hold", nameof(Project), project.Id.ToString(), $"إيقاف مؤقت للمشروع: {project.Name}");
            TempData["Success"] = "تم إيقاف المشروع مؤقتاً";
            return RedirectToAction("Details", new { id });
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            project.Status = ProjectStatus.Cancelled;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Cancel", nameof(Project), project.Id.ToString(), $"إلغاء المشروع: {project.Name}");
            TempData["Success"] = "تم إلغاء المشروع";
            return RedirectToAction("Details", new { id });
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            project.Status = project.CompletionPercentage > 0 ? ProjectStatus.InProgress : ProjectStatus.New;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Reactivate", nameof(Project), project.Id.ToString(), $"إعادة تفعيل المشروع: {project.Name}");
            TempData["Success"] = "تم إعادة تفعيل المشروع";
            return RedirectToAction("Details", new { id });
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
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            if (project.ChildProjects.Any())
            {
                TempData["Error"] = "لا يمكن حذف المشروع لوجود مشاريع فرعية مرتبطة به";
                return RedirectToAction("Index");
            }

            var hasFinancialRecords = await _context.FinancialRecords.AnyAsync(f => f.ProjectId == id);
            if (hasFinancialRecords)
            {
                TempData["Error"] = "لا يمكن حذف المشروع لوجود سجلات مالية مرتبطة به";
                return RedirectToAction("Index");
            }

            var hasSites = await _context.Sites.AnyAsync(s => s.ProjectId == id);
            if (hasSites)
            {
                TempData["Error"] = "لا يمكن حذف المشروع لوجود مواقع ميدانية مرتبطة به";
                return RedirectToAction("Index");
            }

            // تنظيف روابط تبعيات المهام أولاً (علاقتها Restrict) قبل حذف المشروع بالكامل
            var taskIds = project.Tasks.Select(t => t.Id).ToList();
            var dependencyLinks = await _context.TaskDependencies
                .Where(d => taskIds.Contains(d.TaskId) || taskIds.Contains(d.DependsOnTaskId))
                .ToListAsync();
            _context.TaskDependencies.RemoveRange(dependencyLinks);

            var projectName = project.Name;
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(Project), id.ToString(), $"حذف مشروع: {projectName}");

            TempData["Success"] = $"تم حذف المشروع {projectName} بنجاح";
            return RedirectToAction("Index");
        }

        // ============================================
        // الجدول الزمني (ProjectTimeline / Gantt)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTimeline(
            [Bind("ProjectId,Title,Description,StartDate,EndDate,Color,Type")] ProjectTimeline model)
        {
            var project = await _context.Projects.FindAsync(model.ProjectId);
            if (project == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات الحدث الزمني غير صحيحة";
                return RedirectToAction("Details", new { id = model.ProjectId });
            }

            _context.ProjectTimelines.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة الحدث الزمني بنجاح";
            return RedirectToAction("Details", new { id = model.ProjectId });
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTimeline(int id, int projectId)
        {
            var entry = await _context.ProjectTimelines.FindAsync(id);
            if (entry != null)
            {
                _context.ProjectTimelines.Remove(entry);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "تم حذف الحدث الزمني";
            return RedirectToAction("Details", new { id = projectId });
        }

        // ============================================
        // دوال مساعدة
        // ============================================
        private async Task<IQueryable<Project>> ApplyVisibilityFilterAsync(IQueryable<Project> query)
        {
            if (await _permissionService.HasPermissionAsync(User, "Projects.ViewAll"))
                return query;

            var userId = CurrentUserId;
            var myProjectIds = await _context.ProjectTeamMembers
                .Where(tm => tm.UserId == userId)
                .Select(tm => tm.ProjectId)
                .ToListAsync();

            return query.Where(p => p.CreatedById == userId || myProjectIds.Contains(p.Id));
        }

        private async Task<bool> CanAccessProjectAsync(Project project)
        {
            if (await _permissionService.HasPermissionAsync(User, "Projects.ViewAll"))
                return true;

            var userId = CurrentUserId;
            if (project.CreatedById == userId)
                return true;

            return await _context.ProjectTeamMembers.AnyAsync(tm => tm.ProjectId == project.Id && tm.UserId == userId);
        }

        private async Task<bool> CanViewClientAsync()
        {
            return await _permissionService.HasPermissionAsync(User, "Projects.ViewAll")
                || await _permissionService.HasPermissionAsync(User, "PR.Clients");
        }

        private async Task LoadDropdownsAsync(int? excludeProjectId = null)
        {
            var parentQuery = _context.Projects.Where(p => p.Scope == ProjectScope.Main);
            if (excludeProjectId.HasValue)
                parentQuery = parentQuery.Where(p => p.Id != excludeProjectId.Value);

            ViewBag.ParentProjects = await parentQuery.OrderBy(p => p.Name).ToListAsync();
            ViewBag.Clients = await _context.Clients.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.CanViewClient = await CanViewClientAsync();
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