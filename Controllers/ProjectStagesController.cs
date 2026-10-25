using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class ProjectStagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ProjectCalculationService _calc;
        private readonly NotificationService _notify;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PermissionService _permissionService;
        private readonly AuditService _audit;

        public ProjectStagesController(
            AppDbContext context,
            ProjectCalculationService calc,
            NotificationService notify,
            UserManager<ApplicationUser> userManager,
            PermissionService permissionService,
            AuditService audit)
        {
            _context = context;
            _calc = calc;
            _notify = notify;
            _userManager = userManager;
            _permissionService = permissionService;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // تتبّع مراحل مشروع (اختيار مشروع → إحصائيات مراحله)
        // ============================================
        [RequirePermission("Projects.ViewOwn", "Projects.ViewAll")]
        public async Task<IActionResult> Overview(int? projectId, int? subProjectId)
        {
            var canViewAll = await _permissionService.HasPermissionAsync(User, "Projects.ViewAll");
            var myProjectIds = await _context.ProjectTeamMembers
                .Where(tm => tm.UserId == CurrentUserId)
                .Select(tm => tm.ProjectId)
                .ToListAsync();

            var accessibleProjectsQuery = _context.Projects.Where(p => p.ParentProjectId == null).AsQueryable();
            if (!canViewAll)
            {
                accessibleProjectsQuery = accessibleProjectsQuery.Where(p => p.CreatedById == CurrentUserId || myProjectIds.Contains(p.Id));
            }
            ViewBag.Projects = await accessibleProjectsQuery.OrderBy(p => p.Name).ToListAsync();
            ViewBag.ProjectId = projectId;
            ViewBag.SubProjectId = subProjectId;
            ViewBag.StageTemplates = await _context.StageTemplates.Include(t => t.Discipline).ToListAsync();

            List<ProjectStage> stages;

            if (!projectId.HasValue)
            {
                ViewBag.SubProjects = new List<Project>();

                var accessibleProjectIdsQuery = _context.Projects.AsQueryable();
                if (!canViewAll)
                {
                    accessibleProjectIdsQuery = accessibleProjectIdsQuery.Where(p => p.CreatedById == CurrentUserId || myProjectIds.Contains(p.Id));
                }
                var accessibleProjectIds = await accessibleProjectIdsQuery.Select(p => p.Id).ToListAsync();

                stages = await _context.ProjectStages
                    .Include(s => s.AssignedEngineer)
                    .Include(s => s.Assignments)
                    .Include(s => s.Project)
                    .Where(s => accessibleProjectIds.Contains(s.ProjectId))
                    .OrderBy(s => s.Project.Name).ThenBy(s => s.Sequence)
                    .ToListAsync();
            }
            else
            {
                ViewBag.SubProjects = await _context.Projects
                    .Where(p => p.ParentProjectId == projectId.Value)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var effectiveProjectId = subProjectId ?? projectId.Value;

                var canAccessThisProject = canViewAll || await _context.Projects.AnyAsync(p => p.Id == effectiveProjectId && (p.CreatedById == CurrentUserId)) || myProjectIds.Contains(effectiveProjectId);
                if (!canAccessThisProject)
                    return Forbid();

                stages = await _context.ProjectStages
         .Include(s => s.AssignedEngineer)
         .Include(s => s.Assignments)
         .Include(s => s.Project)
         .Where(s => s.ProjectId == effectiveProjectId)
                    .OrderBy(s => s.Sequence)
                    .ToListAsync();
            }
            ViewBag.TotalStages = stages.Count;
            ViewBag.CompletedStages = stages.Count(s => s.Status == StageStatus.Completed);
            ViewBag.InProgressStages = stages.Count(s => s.Status == StageStatus.InProgress);
            ViewBag.DelayedStages = stages.Count(s => s.Status == StageStatus.Delayed);
            var totalStageWeight = stages.Sum(s => s.Weight);
            var distinctProjectCount = stages.Select(s => s.ProjectId).Distinct().Count();
            ViewBag.WeightSum = distinctProjectCount > 0 ? totalStageWeight / distinctProjectCount : 0;
            ViewBag.WeightedCompletion = totalStageWeight > 0
     ? Math.Round(stages.Sum(s => s.Weight * s.CompletionPercentage) / totalStageWeight, 1)
     : 0;
            return View(stages);
        }



        // ============================================
        // تفعيل قالب مرحلة جاهز (بند P1) مع اختيار مهامه الافتراضية + مهام إضافية


        // ============================================
        // تفعيل قالب مرحلة جاهز (بند P1) مع اختيار مهامه الافتراضية + مهام إضافية
        // ============================================
        [RequirePermission("Projects.Stages.Manage")]
            [HttpPost]
            [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateTemplate(
                int projectId, int stageTemplateId, decimal weight, string? assignedEngineerId,
                decimal? area, decimal? pricePerMeter, int? sequence,
                List<int>? selectedTaskIds, string? extraTasks)
        {
            var project = await _context.Projects.Include(p => p.Stages).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId))
                return Forbid();

            var template = await _context.StageTemplates.Include(t => t.DefaultTasks).FirstOrDefaultAsync(t => t.Id == stageTemplateId);

            IActionResult Fail(string error)
            {
                TempData["Error"] = error;
                TempData["FailedTemplateId"] = stageTemplateId;
                TempData["FailedWeight"] = weight.ToString(System.Globalization.CultureInfo.InvariantCulture);
                TempData["FailedArea"] = area?.ToString(System.Globalization.CultureInfo.InvariantCulture);
                TempData["FailedPricePerMeter"] = pricePerMeter?.ToString(System.Globalization.CultureInfo.InvariantCulture);
                TempData["FailedSequence"] = sequence?.ToString(System.Globalization.CultureInfo.InvariantCulture);
                TempData["FailedEngineerId"] = assignedEngineerId;
                TempData["FailedExtraTasks"] = extraTasks;
                return this.RedirectKeepingTab("Details", "Projects", new { id = projectId });
            }

            if (project.Stages.Any(s => s.Name == template.Name))
                return Fail($"مرحلة {template.Name} مفعّلة بالفعل لهذا المشروع");

            var currentTotal = project.Stages.Sum(s => s.Weight);
            if (currentTotal + weight > 100)
                return Fail($"مجموع أوزان المراحل سيتجاوز 100% (المجموع الحالي: {currentTotal}%)");

            var taskWeights = new Dictionary<int, decimal>();
            decimal selectedTasksWeightTotal = 0;
            if (selectedTaskIds != null)
            {
                foreach (var taskId in selectedTaskIds)
                {
                    decimal.TryParse(Request.Form[$"taskWeight_{taskId}"], out var w);
                    taskWeights[taskId] = w;
                    selectedTasksWeightTotal += w;
                }

                if (taskWeights.Values.Any(w => w <= 0))
                    return Fail("كل تكليف (مهمة) مختار يجب أن يكون له وزن أكبر من صفر");
            }

            if (selectedTasksWeightTotal != 100)
                return Fail($"مجموع أوزان المهام المختارة يجب أن يساوي 100% بالضبط (المجموع الحالي: {selectedTasksWeightTotal}%)");

            var stage = new ProjectStage
            {
                ProjectId = projectId,
                Name = template.Name,
                Weight = weight,
                Area = area,
                AssignedEngineerId = string.IsNullOrEmpty(assignedEngineerId) ? null : assignedEngineerId,
                Sequence = sequence ?? (project.Stages.Any() ? project.Stages.Max(s => s.Sequence) + 1 : 1),
                Status = StageStatus.New,
                CompletionPercentage = 0,
                ActualCost = 0,
                PlannedStartDate = project.PlannedStartDate,
                PlannedEndDate = project.PlannedEndDate
            };

            _context.ProjectStages.Add(stage);
            if (stage.AssignedEngineerId != null)
            {
                await EnsureTeamMembershipAsync(projectId, stage.AssignedEngineerId);
            }
            await _context.SaveChangesAsync();

            if (selectedTaskIds != null)
            {
                foreach (var taskId in selectedTaskIds)
                {
                    var defaultTask = template.DefaultTasks.FirstOrDefault(t => t.Id == taskId);
                    if (defaultTask == null) continue;

                    _context.ProjectTasks.Add(new ProjectTask
                    {
                        ProjectId = projectId,
                        StageId = stage.Id,
                        Title = defaultTask.TaskName,
                        Weight = taskWeights.TryGetValue(taskId, out var tw) ? tw : 0,
                        Status = ProjectTaskStatus.NotStarted,
                        Priority = TaskPriority.Medium,
                        CreatedAt = DateTime.UtcNow,
                        CreatedById = CurrentUserId
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(extraTasks))
            {
                foreach (var line in extraTasks.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var title = line.Trim();
                    if (string.IsNullOrEmpty(title)) continue;

                    _context.ProjectTasks.Add(new ProjectTask
                    {
                        ProjectId = projectId,
                        StageId = stage.Id,
                        Title = title,
                        Weight = 0,
                        Status = ProjectTaskStatus.NotStarted,
                        Priority = TaskPriority.Medium,
                        CreatedAt = DateTime.UtcNow,
                        CreatedById = CurrentUserId
                    });
                }
            }

            await _context.SaveChangesAsync();
            await _calc.RecalculateStageAsync(stage.Id);

            TempData["Success"] = $"تم تفعيل مرحلة {template.Name} بنجاح — لا تنسي ضبط وزن المهام الإضافية (وزنها 0 افتراضياً) من شاشة كل مهمة";
            return this.RedirectKeepingTab("Details", "Projects", new { id = projectId });
        }
        // ============================================
        // تعديل مرحلة (لا يمكن تعديل الوزن بعد الإنشاء)
        // ============================================
        [RequirePermission("Projects.Stages.Manage")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var stage = await _context.ProjectStages.FindAsync(id);
            if (stage == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, stage.ProjectId))
                return Forbid();

            await LoadDropdownsAsync(stage.ProjectId);
            return View(stage);
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
      [Bind("Name,Sequence,AssignedEngineerId,PlannedStartDate,PlannedEndDate,ActualDeliveryDate,WorkDocumentation,Area,Weight")] ProjectStage model)
        {
            var stage = await _context.ProjectStages.Include(s => s.Tasks).FirstOrDefaultAsync(s => s.Id == id);
            if (stage == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, stage.ProjectId))
                return Forbid();

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(stage.ProjectId);
                return View(model);
            }

            if (model.Weight != stage.Weight)
            {
                var otherStagesWeightTotal = await _context.ProjectStages
                    .Where(s => s.ProjectId == stage.ProjectId && s.Id != stage.Id)
                    .SumAsync(s => s.Weight);
                if (otherStagesWeightTotal + model.Weight > 100)
                {
                    TempData["Error"] = $"سيتجاوز مجموع أوزان مراحل المشروع 100% (الوزن المتاح حاليًا: {100 - otherStagesWeightTotal:N0}%)";
                    await LoadDropdownsAsync(stage.ProjectId);
                    return View(model);
                }
                var tasksWeightTotal = stage.Tasks.Sum(t => t.Weight);
                if (model.Weight < tasksWeightTotal)
                {
                    TempData["Error"] = $"لا يمكن أن يكون وزن المرحلة أقل من مجموع أوزان مهامها الحالية ({tasksWeightTotal:N0}%)";
                    await LoadDropdownsAsync(stage.ProjectId);
                    return View(model);
                }
                stage.Weight = model.Weight;
            }

            var wasCompleted = stage.Status == StageStatus.Completed;

            stage.Name = model.Name;
            stage.Sequence = model.Sequence;
            stage.AssignedEngineerId = model.AssignedEngineerId;
            if (!string.IsNullOrEmpty(model.AssignedEngineerId))
            {
                await EnsureTeamMembershipAsync(stage.ProjectId, model.AssignedEngineerId);
            }
            stage.PlannedStartDate = model.PlannedStartDate;
            stage.PlannedEndDate = model.PlannedEndDate;
            stage.ActualDeliveryDate = model.ActualDeliveryDate;
            stage.WorkDocumentation = model.WorkDocumentation;
            stage.Area = model.Area;
    
            var allStages = await _context.ProjectStages.Include(s => s.Tasks).Where(s => s.ProjectId == stage.ProjectId).ToListAsync();
            await _calc.SyncStageRedesignPercentageAsync(stage.Id);
            foreach (var s in allStages)
            {
                _calc.ApplyAutomaticStageStatus(s, allStages);
            }

            await _context.SaveChangesAsync();
            await _calc.RecalculateProjectAsync(stage.ProjectId);

            // إشعار اكتمال المرحلة — لكل من يملك صلاحية تخص المشاريع/المواقع (القسم 10 بند 3)
            if (!wasCompleted && stage.Status == StageStatus.Completed)
            {
                var recipientIds = await _permissionService.GetProjectRecipientsAsync(stage.ProjectId);

                await _notify.NotifyManyAsync(recipientIds, $"اكتملت المرحلة: {stage.Name}", NotificationEventType.StageCompleted, $"/Projects/Details/{stage.ProjectId}", entityType: "ProjectStage", entityId: stage.Id);
            }

            TempData["Success"] = $"تم تحديث المرحلة {stage.Name} بنجاح";
            return this.RedirectKeepingTab("Details", "Projects", new { id = stage.ProjectId });
        }

        // ============================================
        // حذف مرحلة (يحذف خطواتها ومهامها تلقائياً عبر Cascade)
        // ============================================
        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var stage = await _context.ProjectStages.Include(s => s.Assignments).FirstOrDefaultAsync(s => s.Id == id);
            if (stage == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, stage.ProjectId))
                return Forbid();

            if (stage.Assignments.Any(a => a.IsTransferredToFinance))
            {
                TempData["Error"] = "لا يمكن حذف المرحلة لوجود تكليف تم ترحيله للمالية بالفعل ضمنها";
                return this.RedirectKeepingTab("Details", "Projects", new { id = stage.ProjectId });
            }

            var projectId = stage.ProjectId;

            var taskIds = await _context.ProjectTasks.Where(t => t.StageId == id).Select(t => t.Id).ToListAsync();
            var dependencyLinks = await _context.TaskDependencies
                .Where(d => taskIds.Contains(d.TaskId) || taskIds.Contains(d.DependsOnTaskId))
                .ToListAsync();
            _context.TaskDependencies.RemoveRange(dependencyLinks);

            _context.ProjectStages.Remove(stage);
            await _context.SaveChangesAsync();

            var remainingStages = await _context.ProjectStages.Include(s => s.Tasks).Where(s => s.ProjectId == projectId).ToListAsync();
            foreach (var s in remainingStages)
            {
                _calc.ApplyAutomaticStageStatus(s, remainingStages);
            }
            await _context.SaveChangesAsync();

            await _calc.RecalculateProjectAsync(projectId);

            TempData["Success"] = "تم حذف المرحلة بنجاح";
            return this.RedirectKeepingTab("Details", "Projects", new { id = projectId });
        }

        private async Task LoadDropdownsAsync(int projectId)
        {
            ViewBag.Engineers = await _userManager.Users
        .Where(u => u.IsActive)
         .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
         .ToListAsync();
        }
        private async Task EnsureTeamMembershipAsync(int projectId, string userId)
        {
            var exists = await _context.ProjectTeamMembers.AnyAsync(tm => tm.ProjectId == projectId && tm.UserId == userId);
            if (!exists)
            {
                _context.ProjectTeamMembers.Add(new ProjectTeamMember
                {
                    ProjectId = projectId,
                    UserId = userId,
                    Role = TeamRole.Engineer,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }
        // أرشفة مباشرة لمرحلة كاملة — لمدير النظام فقط، تُنهي كل تكليفاتها غير المكتملة/الملغاة تلقائياً وترحّلها للمالية
        [Authorize(Roles = "مدير النظام")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveStage(int id)
        {
            var stage = await _context.ProjectStages
                .Include(s => s.Assignments).ThenInclude(a => a.Tasks)
                .Include(s => s.Assignments).ThenInclude(a => a.Engineers)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stage == null)
                return NotFound();

            if (stage.Status == StageStatus.Completed)
            {
                TempData["Error"] = "المرحلة مكتملة بالفعل";
                return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
            }

            if (!stage.Assignments.Any())
            {
                TempData["Error"] = "لا يمكن أرشفة مرحلة لم تُضَف لها أي تكليفات بعد";
                return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
            }

            var assignmentsToComplete = stage.Assignments
                .Where(a => a.Status != AssignmentStatus.Completed && a.Status != AssignmentStatus.Cancelled)
                .ToList();

            var missingEngineerAssignment = assignmentsToComplete.FirstOrDefault(a => !a.Engineers.Any());
            if (missingEngineerAssignment != null)
            {
                TempData["Error"] = $"لا يمكن أرشفة المرحلة — التكليف \"{missingEngineerAssignment.AssignmentType}\" بدون مهندسة مرتبطة به";
                return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
            }

            var today = DateTime.UtcNow.Date;
            foreach (var assignment in assignmentsToComplete)
            {
                foreach (var task in assignment.Tasks)
                {
                    task.Status = ProjectTaskStatus.Completed;
                    task.CompletionPercentage = 100;
                    if (!task.ActualDeliveryDate.HasValue)
                        task.ActualDeliveryDate = today;
                }
                assignment.Status = AssignmentStatus.Completed;
            }
            await _context.SaveChangesAsync();

            foreach (var assignment in assignmentsToComplete)
            {
                await _calc.RecalculateAssignmentFinanceAsync(assignment.Id);
            }
            await _calc.RecalculateStageAsync(id);

            await _audit.LogAsync(CurrentUserId, "ArchiveStage", nameof(ProjectStage), id.ToString(), $"أرشفة مرحلة مباشرة بواسطة مدير النظام — تم إنهاء {assignmentsToComplete.Count} تكليف(ات) تلقائياً وترحيلها للمالية");

            TempData["Success"] = $"تم أرشفة المرحلة، وإنهاء {assignmentsToComplete.Count} تكليف(ات) مرتبطة بها تلقائياً وترحيلها للمالية";
            return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
        }
    }
}