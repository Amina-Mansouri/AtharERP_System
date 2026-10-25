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
        private readonly ProjectCalculationService _calc;
        private readonly SiteCalculationService _siteCalc;

        public ProjectsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            PermissionService permissionService,
            AuditService audit,
            ProjectCalculationService calc,
            SiteCalculationService siteCalc)
        {
            _context = context;
            _userManager = userManager;
            _permissionService = permissionService;
            _audit = audit;
            _calc = calc;
            _siteCalc = siteCalc;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة المشاريع + عزل الرؤية (القسم 6.6.2)
        // ============================================
        [RequirePermission("Projects.ViewOwn", "Projects.ViewAll")]
        public async Task<IActionResult> Index(string? search, ProjectStatus? status, ProjectScope? scope, Priority? priority, int page = 1)
        {
            ViewBag.Page = page;
            var baseQuery = _context.Projects
    .Include(p => p.Client)
    .Include(p => p.ParentProject)
    .Include(p => p.ChildProjects)
    .Include(p => p.ProjectCategory)
    .AsQueryable();

            baseQuery = await ApplyVisibilityFilterAsync(baseQuery);

            var today = DateTime.UtcNow.Date;
            var soonCutoff = today.AddDays(30);

            ViewBag.TotalProjects = await baseQuery.CountAsync();
            ViewBag.InProgressProjects = await baseQuery.CountAsync(p => p.Status == ProjectStatus.InProgress);
            ViewBag.CompletedProjects = await baseQuery.CountAsync(p => p.Status == ProjectStatus.Completed);
            ViewBag.OnHoldProjects = await baseQuery.CountAsync(p => p.Status == ProjectStatus.OnHold);
            ViewBag.SoonDeliveryProjects = await baseQuery.CountAsync(p => p.Status == ProjectStatus.InProgress && p.PlannedEndDate != null && p.PlannedEndDate >= today && p.PlannedEndDate <= soonCutoff);
            ViewBag.DelayedProjects = await baseQuery.CountAsync(p => p.Status == ProjectStatus.Delayed);

            var query = baseQuery;

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Code.Contains(search));
            }

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (scope.HasValue)
                query = query.Where(p => p.Scope == scope.Value);

            if (priority.HasValue)
                query = query.Where(p => p.Priority == priority.Value);

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Scope = scope;
            ViewBag.Priority = priority;
            ViewBag.CanViewClient = await CanViewClientAsync();

            var activeProjectIds = await baseQuery
     .Where(p => p.Status != ProjectStatus.New && p.Status != ProjectStatus.OnHold && p.Status != ProjectStatus.Cancelled)
     .Select(p => p.Id)
     .ToListAsync();
            ViewBag.ActiveProjectsForStageChart = activeProjectIds.Count;
            var stageStatusGroups = await _context.ProjectStages
                .Where(s => activeProjectIds.Contains(s.ProjectId))
                .GroupBy(s => s.Name)
                .Select(g => new
                {
                    StageName = g.Key,
                    New = g.Count(s => s.Status == StageStatus.New),
                    InProgress = g.Count(s => s.Status == StageStatus.InProgress),
                    ClientReview = g.Count(s => s.Status == StageStatus.ClientReview),
                    Completed = g.Count(s => s.Status == StageStatus.Completed),
                    Delayed = g.Count(s => s.Status == StageStatus.Delayed)
                })
                .ToListAsync();

            ViewBag.StageStatusRows = stageStatusGroups
                .OrderByDescending(x => x.New + x.InProgress + x.ClientReview + x.Completed + x.Delayed)
                .Select(x => (x.StageName, x.New, x.InProgress, x.ClientReview, x.Completed, x.Delayed))
                .ToList();
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
                .Include(p => p.ChildProjects).ThenInclude(c => c.Stages).ThenInclude(s => s.Tasks)
                .Include(p => p.ChildProjects).ThenInclude(c => c.Stages).ThenInclude(s => s.Assignments)
                .Include(p => p.CreatedBy)
                .Include(p => p.TeamMembers).ThenInclude(tm => tm.User)
                .Include(p => p.Timelines)
                .Include(p => p.Stages).ThenInclude(s => s.AssignedEngineer)

                .Include(p => p.Stages).ThenInclude(s => s.Assignments).ThenInclude(a => a.Engineers).ThenInclude(e => e.User)
                .Include(p => p.Stages).ThenInclude(s => s.Assignments).ThenInclude(a => a.Subtasks)
                .Include(p => p.Stages).ThenInclude(s => s.Assignments).ThenInclude(a => a.Tasks).ThenInclude(t => t.Todos).ThenInclude(td => td.DesignProposals).ThenInclude(dp => dp.PreparedBy)
               .Include(p => p.Stages).ThenInclude(s => s.Assignments).ThenInclude(a => a.FinancialRecords)
                .Include(p => p.Stages).ThenInclude(s => s.Assignments).ThenInclude(a => a.FinancialClaims)
               .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            if (!await CanAccessProjectAsync(project))
                return Forbid();
            // بيانات مالية في بطاقة "بيانات المشروع": عند الرئيسي تُجمَّع مع كل الفروع، وعند الفرعي تخصّه هو فقط (القسم 2.2)
            if (project.Scope == ProjectScope.Main)
            {
                var childIds = project.ChildProjects.Select(c => c.Id).ToList();
                if (childIds.Any())
                {
                    var aggregatedTasks = await _context.ProjectTasks
                        .Where(t => childIds.Contains(t.ProjectId))
                        .ToListAsync();
                    ViewBag.AggregatedSubProjectTasks = aggregatedTasks;
                }

                var childrenCost = childIds.Any() ? await _context.Projects.Where(p => childIds.Contains(p.Id)).SumAsync(p => p.ActualCost) : 0;
                var childrenBudget = childIds.Any() ? await _context.Projects.Where(p => childIds.Contains(p.Id)).SumAsync(p => p.Budget ?? 0) : 0;

                ViewBag.AggregatedActualCost = project.ActualCost + childrenCost;
                ViewBag.AggregatedBudget = (project.Budget ?? 0) + childrenBudget;

                var allProjectIdsForExpenses = new List<int> { project.Id };
                allProjectIdsForExpenses.AddRange(childIds);
                ViewBag.AggregatedProjectExpenses = await _context.ProjectExpenses
                    .Where(e => e.ProjectId.HasValue && allProjectIdsForExpenses.Contains(e.ProjectId.Value) && e.SiteId == null)
                    .SumAsync(e => e.Amount);

                var siteCostBreakdown = await _siteCalc.CalculateProjectSiteCostsBreakdownAsync(allProjectIdsForExpenses);
                ViewBag.AggregatedSiteCosts = siteCostBreakdown.Total;
            }
            else
            {
                ViewBag.AggregatedActualCost = project.ActualCost;
                ViewBag.AggregatedBudget = project.Budget ?? 0;

                ViewBag.AggregatedProjectExpenses = await _context.ProjectExpenses
                    .Where(e => e.ProjectId == project.Id && e.SiteId == null)
                    .SumAsync(e => e.Amount);

                var siteCostBreakdown = await _siteCalc.CalculateProjectSiteCostsBreakdownAsync(new List<int> { project.Id });
                ViewBag.AggregatedSiteCosts = siteCostBreakdown.Total;
            }

            ViewBag.CanViewClient = await CanViewClientAsync();
            ViewBag.CanViewCosts = await CanViewCostsAsync(project);
            ViewBag.CanEdit = await _permissionService.HasPermissionAsync(User, "Projects.Edit");
            ViewBag.AllEmployees = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
            ViewBag.Engineers = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.StageDisciplines = await _context.Departments
   .Where(d => d.ParentDepartment != null && d.ParentDepartment.IsStageDisciplinesParent && d.IsActive)
   .OrderBy(d => d.Name)
   .ToListAsync();
            ViewBag.StageTemplates = await _context.StageTemplates.Include(t => t.DefaultTasks).OrderBy(t => t.Order).ToListAsync();
            ViewBag.Documents = await _context.ProjectDocuments.Include(d => d.UploadedBy).Where(d => d.ProjectId == id).OrderByDescending(d => d.UploadedAt).ToListAsync();
            await LoadDropdownsAsync(id);
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
            ViewBag.LastProjectCode = await _context.Projects
       .Where(p => p.Scope == ProjectScope.Main)
       .OrderByDescending(p => p.Id)
       .Select(p => p.Code)
       .FirstOrDefaultAsync();
            ViewBag.SuggestedProjectCode = await GenerateSuggestedMainProjectCodeAsync();
            ViewBag.CanEdit = true;
            ViewBag.CanViewCosts = await _permissionService.HasPermissionAsync(User, "Finance.Costs.View");
            ViewBag.Engineers = new List<ApplicationUser>();
            ViewBag.StageTemplates = await _context.StageTemplates.Include(t => t.DefaultTasks).OrderBy(t => t.Order).ToListAsync();
            ViewBag.Documents = new List<ProjectDocument>();
            return View("Details", new Project());
        }

        [RequirePermission("Projects.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
                     [Bind("Name,Description,ClientId,ParentProjectId,Scope,Type,Code,PlannedStartDate,PlannedEndDate,ActualDeliveryDate,Priority,AutoTransferToSite,ProjectCategoryId")] Project model)
        {
            if (model.PlannedEndDate.HasValue && model.PlannedStartDate.HasValue && model.PlannedEndDate < model.PlannedStartDate)
            {
                ModelState.AddModelError(string.Empty, "تاريخ التسليم المتفق عليه يجب أن يكون بعد تاريخ البدء");
            }

            if (model.Scope == ProjectScope.Sub && model.ParentProjectId.HasValue)
            {
                var parentForDateCheck = await _context.Projects.FindAsync(model.ParentProjectId.Value);
                if (parentForDateCheck != null)
                {
                    if (parentForDateCheck.PlannedStartDate.HasValue && model.PlannedStartDate.HasValue && model.PlannedStartDate.Value < parentForDateCheck.PlannedStartDate.Value)
                    {
                        ModelState.AddModelError(string.Empty, $"تاريخ بداية المبنى لا يمكن أن يكون قبل تاريخ بداية المشروع الرئيسي ({parentForDateCheck.PlannedStartDate.Value:yyyy-MM-dd})");
                    }
                    if (parentForDateCheck.PlannedEndDate.HasValue && model.PlannedEndDate.HasValue && model.PlannedEndDate.Value > parentForDateCheck.PlannedEndDate.Value)
                    {
                        ModelState.AddModelError(string.Empty, $"تاريخ نهاية المبنى لا يمكن أن يتجاوز تاريخ نهاية المشروع الرئيسي ({parentForDateCheck.PlannedEndDate.Value:yyyy-MM-dd})");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                if (model.Scope == ProjectScope.Sub && model.ParentProjectId.HasValue)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
                    TempData["Error"] = firstError ?? "تعذّر إضافة المبنى — تحقّقي من البيانات المدخلة";
                    return RedirectToAction("Details", new { id = model.ParentProjectId.Value, tab = "pane-subprojects" });
                }

                await LoadDropdownsAsync();
                ViewBag.CanEdit = true;
                ViewBag.StageTemplates = await _context.StageTemplates.Include(t => t.DefaultTasks).OrderBy(t => t.Order).ToListAsync();
                ViewBag.Engineers = new List<ApplicationUser>();
                ViewBag.Documents = new List<ProjectDocument>();
                ViewBag.SuggestedProjectCode = await GenerateSuggestedMainProjectCodeAsync();
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
           [Bind("Name,Description,ClientId,ParentProjectId,Scope,Type,Code,PlannedStartDate,PlannedEndDate,ActualDeliveryDate,Priority,AutoTransferToSite,ProjectCategoryId")] Project model)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            if (!await CanAccessProjectAsync(project))
                return Forbid();

            if (model.ParentProjectId == id)
            {
                ModelState.AddModelError(string.Empty, "لا يمكن أن يكون المشروع أباً لنفسه");
            }

            if (model.PlannedEndDate.HasValue && model.PlannedStartDate.HasValue && model.PlannedEndDate < model.PlannedStartDate)
            {
                ModelState.AddModelError(string.Empty, "تاريخ التسليم المتفق عليه يجب أن يكون بعد تاريخ البدء");
            }

            if (string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError(string.Empty, "رمز المشروع/رقم المبنى مطلوب");
            }
            else if (model.Code != project.Code && await _context.Projects.AnyAsync(p => p.Code == model.Code && p.Id != id))
            {
                ModelState.AddModelError(string.Empty, "هذا الرمز/الرقم مستخدم بالفعل من مشروع آخر، اختاري رمزاً مختلفاً");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(id);
                model.Id = id;
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

            project.Code = model.Code;
            project.Name = model.Name;
            project.Description = model.Description;
            project.ClientId = model.ClientId;
            project.ParentProjectId = model.ParentProjectId;
            project.Scope = model.Scope;
            project.Type = model.Type;
            project.ProjectCategoryId = model.ProjectCategoryId;
            project.PlannedStartDate = model.PlannedStartDate;
            project.PlannedEndDate = model.PlannedEndDate;
            project.ActualDeliveryDate = model.ActualDeliveryDate;

            project.Priority = model.Priority;
            project.AutoTransferToSite = model.AutoTransferToSite;

            await _context.SaveChangesAsync();

            var projectStagesToSync = await _context.ProjectStages.Where(s => s.ProjectId == project.Id).ToListAsync();
            if (projectStagesToSync.Any())
            {
                foreach (var s in projectStagesToSync)
                {
                    s.PlannedStartDate = project.PlannedStartDate;
                    s.PlannedEndDate = project.PlannedEndDate;
                }
                await _context.SaveChangesAsync();

                foreach (var s in projectStagesToSync)
                {
                    await _calc.RecalculateStageAsync(s.Id);
                }
            }
            else
            {
                await _calc.RecalculateProjectAsync(project.Id);
            }

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

            if (!await CanAccessProjectAsync(project))
                return Forbid();

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

            if (!await CanAccessProjectAsync(project))
                return Forbid();

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

            if (!await CanAccessProjectAsync(project))
                return Forbid();

            project.Status = project.CompletionPercentage > 0 ? ProjectStatus.InProgress : ProjectStatus.New;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Reactivate", nameof(Project), project.Id.ToString(), $"إعادة تفعيل المشروع: {project.Name}");
            TempData["Success"] = "تم إعادة تفعيل المشروع";
            return RedirectToAction("Details", new { id });
        }
        // ============================================
        // حذف مشروع بالكامل — مع كل تبعياته والمشاريع الفرعية منه، بلا استثناء
        // ============================================
        [RequirePermission("Projects.Delete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            if (!await CanAccessProjectAsync(project))
                return Forbid();

            var allProjectIds = await GetProjectAndDescendantIdsAsync(id);
            var allSiteIds = await _context.Sites.Where(s => allProjectIds.Contains(s.ProjectId)).Select(s => s.Id).ToListAsync();

            var hasExpenses = await _context.ProjectExpenses.AnyAsync(e =>
                (e.ProjectId.HasValue && allProjectIds.Contains(e.ProjectId.Value)) ||
                (e.SiteId.HasValue && allSiteIds.Contains(e.SiteId.Value)));

            if (hasExpenses)
            {
                TempData["Error"] = "لا يمكن حذف المشروع لوجود مصروفات مسجَّلة عليه أو على أحد مشاريعه الفرعية أو مواقعه — احذفي أو رحّلي هذه المصروفات أولاً";
                return RedirectToAction("Details", new { id });
            }

            var projectName = project.Name;

            using var transaction = await _context.Database.BeginTransactionAsync();
            await DeleteProjectCascadeAsync(id);
            await transaction.CommitAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(Project), id.ToString(), $"حذف مشروع بالكامل مع كل بياناته التابعة والمشاريع الفرعية: {projectName}");

            TempData["Success"] = $"تم حذف المشروع {projectName} وكل بياناته التابعة بنجاح";
            return RedirectToAction("Index");
        }
        private async Task<List<int>> GetProjectAndDescendantIdsAsync(int projectId)
        {
            var ids = new List<int> { projectId };
            var childIds = await _context.Projects.Where(p => p.ParentProjectId == projectId).Select(p => p.Id).ToListAsync();
            foreach (var childId in childIds)
            {
                ids.AddRange(await GetProjectAndDescendantIdsAsync(childId));
            }
            return ids;
        }

        // حذف مشروع واحد بكل تبعياته، ثم استدعاء نفسها لكل مشروع فرعي أولاً (الأبناء قبل الأب)
        private async Task DeleteProjectCascadeAsync(int projectId)
        {
            var childIds = await _context.Projects
                .Where(p => p.ParentProjectId == projectId)
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var childId in childIds)
            {
                await DeleteProjectCascadeAsync(childId);
            }

            var taskIds = await _context.ProjectTasks.Where(t => t.ProjectId == projectId).Select(t => t.Id).ToListAsync();
            var dependencyLinks = await _context.TaskDependencies
                .Where(d => taskIds.Contains(d.TaskId) || taskIds.Contains(d.DependsOnTaskId))
                .ToListAsync();
            _context.TaskDependencies.RemoveRange(dependencyLinks);

            var financialRecords = await _context.FinancialRecords.Where(f => f.ProjectId == projectId).ToListAsync();
            _context.FinancialRecords.RemoveRange(financialRecords);

            // حذف المواقع الميدانية يسحب معه تلقائياً كل ما يتبعها (مراحل، تقارير، فحوصات، مقاولون، طلبات توريد، صيانة، مستندات) عبر Cascade في قاعدة البيانات
            var sites = await _context.Sites.Where(s => s.ProjectId == projectId).ToListAsync();
            _context.Sites.RemoveRange(sites);

            await _context.SaveChangesAsync();

            var project = await _context.Projects.FindAsync(projectId);
            if (project != null)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
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

            if (!await CanAccessProjectAsync(project))
                return Forbid();

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
            if (entry == null)
                return RedirectToAction("Details", new { id = projectId });

            var project = await _context.Projects.FindAsync(entry.ProjectId);
            if (project == null || !await CanAccessProjectAsync(project))
                return Forbid();

            var ownerProjectId = entry.ProjectId;
            _context.ProjectTimelines.Remove(entry);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف الحدث الزمني";
            return RedirectToAction("Details", new { id = ownerProjectId });
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


        private async Task<bool> CanViewCostsAsync(Project project)
        {
            if (!await _permissionService.HasPermissionAsync(User, "Finance.Costs.View"))
                return false;
            return await CanAccessProjectAsync(project);
        }
        private async Task<bool> CanViewClientAsync()
        {
            return await _permissionService.HasPermissionAsync(User, "PR.Clients");
        }

        private async Task LoadDropdownsAsync(int? excludeProjectId = null)
        {
            var parentQuery = _context.Projects.Where(p => p.Scope == ProjectScope.Main);
            if (excludeProjectId.HasValue)
                parentQuery = parentQuery.Where(p => p.Id != excludeProjectId.Value);

            ViewBag.ParentProjects = await parentQuery.OrderBy(p => p.Name).ToListAsync();
            ViewBag.Clients = await _context.Clients.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.CanViewClient = await CanViewClientAsync();
            ViewBag.ProjectCategories = await _context.ProjectCategories.Where(c => c.IsActive).OrderBy(c => c.Classification).ThenBy(c => c.Tier).ToListAsync();

            // تذكير فقط: آخر مشروع فرعي أُضيف تحت كل مشروع رئيسي (لا يؤثر على الترقيم الفعلي)
            ViewBag.LastSubProjectByParent = await _context.Projects
                .Where(p => p.Scope == ProjectScope.Sub && p.ParentProjectId != null)
                .GroupBy(p => p.ParentProjectId!.Value)
                .Select(g => new { ParentId = g.Key, Last = g.OrderByDescending(x => x.Id).First() })
                                .ToDictionaryAsync(x => x.ParentId, x => x.Last.Code);
        }

        private async Task<string> GenerateSuggestedMainProjectCodeAsync()
        {
            var mainCodes = await _context.Projects
                .Where(p => p.Scope == ProjectScope.Main)
                .Select(p => p.Code)
                .ToListAsync();

            var parsed = mainCodes
                .Select(c => System.Text.RegularExpressions.Regex.Match(c ?? "", @"(\d+)(?!.*\d)"))
                .Where(m => m.Success)
                .Select(m => new { Value = int.Parse(m.Value), Width = m.Value.Length })
                .ToList();

            var best = parsed.OrderByDescending(p => p.Value).FirstOrDefault();
            var padWidth = best?.Width ?? 3;
            var nextSeq = (best?.Value ?? 0) + 1;

            return $"P({nextSeq.ToString("D" + padWidth)}) - ";
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