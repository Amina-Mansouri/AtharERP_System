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

            var projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
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
                .Include(p => p.Stages).ThenInclude(s => s.Assignments).ThenInclude(a => a.LeadEngineer)
                .Include(p => p.Stages).ThenInclude(s => s.Assignments).ThenInclude(a => a.AssistantEngineer)
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
            ViewBag.CanViewCosts = await _permissionService.HasPermissionAsync(User, "Projects.Costs.View");
            ViewBag.CanEdit = await _permissionService.HasPermissionAsync(User, "Projects.Edit");
            ViewBag.AllEmployees = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
            ViewBag.Engineers = project.TeamMembers.Select(tm => tm.User).OrderBy(u => u.FullName).ToList();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.StageTemplates = await _context.StageTemplates.Include(t => t.DefaultTasks).OrderBy(t => t.Order).ToListAsync();
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
            return View();
        }

        [RequirePermission("Projects.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
                   [Bind("Name,Description,ClientId,ParentProjectId,Scope,Status,PlannedStartDate,PlannedEndDate,ActualStartDate,ActualEndDate,Budget,Priority,IsUrgent")] Project model)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(model);
            }

            model.Code = await GenerateProjectCodeAsync();
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

        // ============================================
        // تعديل مشروع
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            await LoadDropdownsAsync(id);
            return View(project);
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
                       [Bind("Name,Description,ClientId,ParentProjectId,Scope,Status,PlannedStartDate,PlannedEndDate,ActualStartDate,ActualEndDate,Budget,Priority,IsUrgent")] Project model)
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
                return View(project);
            }

            project.Name = model.Name;
            project.Description = model.Description;
            project.ClientId = model.ClientId;
            project.ParentProjectId = model.ParentProjectId;
            project.Type = model.Type;
            project.Status = model.Status;
            project.PlannedStartDate = model.PlannedStartDate;
            project.PlannedEndDate = model.PlannedEndDate;
            project.ActualStartDate = model.ActualStartDate;
            project.ActualEndDate = model.ActualEndDate;
            project.Budget = model.Budget;
            project.Priority = model.Priority;
            project.IsUrgent = model.IsUrgent;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(Project), project.Id.ToString(), $"تعديل مشروع: {project.Name}");

            TempData["Success"] = $"تم تحديث المشروع {project.Name} بنجاح";
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
        // فريق المشروع (ProjectTeamMember)
        // ============================================
        [RequirePermission("Projects.Edit", "Projects.Stages.Manage", "Projects.Tasks.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeamMember(int projectId, string userId, TeamRole role)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                return NotFound();

            var exists = await _context.ProjectTeamMembers.AnyAsync(tm => tm.ProjectId == projectId && tm.UserId == userId);
            if (!exists)
            {
                _context.ProjectTeamMembers.Add(new ProjectTeamMember
                {
                    ProjectId = projectId,
                    UserId = userId,
                    Role = role,
                    JoinedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "تمت إضافة العضو إلى الفريق بنجاح";
            }
            else
            {
                TempData["Error"] = "هذا العضو موجود بالفعل في الفريق";
            }

            return RedirectToAction("Details", new { id = projectId });
        }

        [RequirePermission("Projects.Edit", "Projects.Stages.Manage", "Projects.Tasks.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTeamMember(int id, int projectId)
        {
            var member = await _context.ProjectTeamMembers.FindAsync(id);
            if (member == null)
                return RedirectToAction("Details", new { id = projectId });

            var isAssignedToAnyTask = await _context.TaskAssignees
                .AnyAsync(a => a.UserId == member.UserId && a.Task.ProjectId == projectId);

            if (isAssignedToAnyTask)
            {
                TempData["Error"] = "لا يمكن إزالة هذا العضو من الفريق لأنه لا يزال مكلَّفاً بمهمة واحدة أو أكثر داخل هذا المشروع. أزيليه من المهام أولاً";
                return RedirectToAction("Details", new { id = projectId });
            }

            _context.ProjectTeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إزالة العضو من الفريق";
            return RedirectToAction("Details", new { id = projectId });
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