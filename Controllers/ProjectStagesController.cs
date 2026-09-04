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
    public class ProjectStagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ProjectCalculationService _calc;
        private readonly NotificationService _notify;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectStagesController(
            AppDbContext context,
            ProjectCalculationService calc,
            NotificationService notify,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _calc = calc;
            _notify = notify;
            _userManager = userManager;
        }

        // ============================================
        // تتبّع تكليفات مشروع (اختيار مشروع ثم مرحلة → إحصائيات تكليفاتها)
        // ============================================
        [RequirePermission("Projects.Assignments.View")]
        public async Task<IActionResult> Overview(int? projectId, int? stageId)
        {
            ViewBag.Projects = await _context.Projects.OrderBy(p => p.Name).ToListAsync();
            ViewBag.ProjectId = projectId;
            ViewBag.StageId = stageId;

            if (!projectId.HasValue)
            {
                ViewBag.Stages = new List<ProjectStage>();
                return View(new List<ProjectAssignment>());
            }

            ViewBag.Stages = await _context.ProjectStages
                .Where(s => s.ProjectId == projectId.Value)
                .OrderBy(s => s.Sequence)
                .ToListAsync();

            var query = _context.ProjectAssignments
                .Include(a => a.Stage)
                .Include(a => a.Engineers).ThenInclude(e => e.User)
                .Where(a => a.ProjectId == projectId.Value);

            if (stageId.HasValue)
                query = query.Where(a => a.StageId == stageId.Value);

            var assignments = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();

            ViewBag.TotalAssignments = assignments.Count;
            ViewBag.PendingAssignments = assignments.Count(a => a.Status == AssignmentStatus.Pending);
            ViewBag.InProgressAssignments = assignments.Count(a => a.Status == AssignmentStatus.InProgress);
            ViewBag.CompletedAssignments = assignments.Count(a => a.Status == AssignmentStatus.Completed);

            var today = DateTime.UtcNow.Date;
            ViewBag.OverdueAssignments = assignments.Count(a => a.Status != AssignmentStatus.Completed && a.AgreedDate.HasValue && a.AgreedDate.Value.Date < today);
            ViewBag.TotalValue = assignments.Sum(a => a.FinalAmount);

            return View(assignments);
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // تتبّع مراحل مشروع (اختيار مشروع → إحصائيات مراحله)
        // ============================================
        [RequirePermission("Projects.ViewOwn", "Projects.ViewAll")]
        public async Task<IActionResult> Overview(int? projectId)
        {
            ViewBag.Projects = await _context.Projects.OrderBy(p => p.Name).ToListAsync();
            ViewBag.ProjectId = projectId;

            if (!projectId.HasValue)
                return View(new List<ProjectStage>());

            var stages = await _context.ProjectStages
                .Include(s => s.AssignedEngineer)
                .Include(s => s.Assignments)
                .Where(s => s.ProjectId == projectId.Value)
                .OrderBy(s => s.Sequence)
                .ToListAsync();

            ViewBag.TotalStages = stages.Count;
            ViewBag.CompletedStages = stages.Count(s => s.Status == StageStatus.Completed);
            ViewBag.InProgressStages = stages.Count(s => s.Status == StageStatus.InProgress);
            ViewBag.DelayedStages = stages.Count(s => s.Status == StageStatus.Delayed);
            ViewBag.WeightSum = stages.Sum(s => s.Weight);
            ViewBag.AvgCompletion = stages.Any() ? Math.Round(stages.Average(s => s.CompletionPercentage), 1) : 0;

            return View(stages);
        }


        // ============================================
        // إنشاء مرحلة جديدة (نموذج مضمّن داخل صفحة تفاصيل المشروع)
        // ============================================
        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProjectId,Name,Sequence,Weight,Cost,AssignedEngineerId,DepartmentId")] ProjectStage model)
        {
            var project = await _context.Projects.Include(p => p.Stages).FirstOrDefaultAsync(p => p.Id == model.ProjectId);
            if (project == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات المرحلة غير صحيحة";
                return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
            }

            // مجموع أوزان المراحل داخل المشروع يجب ألا يتجاوز 100% (القسم 6.1)
            var currentTotal = project.Stages.Sum(s => s.Weight);
            if (currentTotal + model.Weight > 100)
            {
                TempData["Error"] = $"مجموع أوزان المراحل سيتجاوز 100% (المجموع الحالي: {currentTotal}%)";
                return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
            }

            model.Status = StageStatus.New;
            model.CompletionPercentage = 0;
            model.ActualCost = 0;

            _context.ProjectStages.Add(model);
            await _context.SaveChangesAsync();

            await _calc.RecalculateProjectAsync(model.ProjectId);

            TempData["Success"] = $"تمت إضافة المرحلة {model.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
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
                List<int>? selectedTaskIds, string? extraTasks)
        {
                var project = await _context.Projects.Include(p => p.Stages).FirstOrDefaultAsync(p => p.Id == projectId);
                if (project == null)
                    return NotFound();

                var template = await _context.StageTemplates.Include(t => t.DefaultTasks).FirstOrDefaultAsync(t => t.Id == stageTemplateId);
                if (template == null)
                    return NotFound();

                if (project.Stages.Any(s => s.Name == template.Name))
                {
                    TempData["Error"] = $"مرحلة {template.Name} مفعّلة بالفعل لهذا المشروع";
                    return RedirectToAction("Details", "Projects", new { id = projectId });
                }

                var currentTotal = project.Stages.Sum(s => s.Weight);
                if (currentTotal + weight > 100)
                {
                    TempData["Error"] = $"مجموع أوزان المراحل سيتجاوز 100% (المجموع الحالي: {currentTotal}%)";
                    return RedirectToAction("Details", "Projects", new { id = projectId });
                }

            var stage = new ProjectStage
            {
                ProjectId = projectId,
                Name = template.Name,
                Weight = weight,
                AssignedEngineerId = string.IsNullOrEmpty(assignedEngineerId) ? null : assignedEngineerId,
                Sequence = project.Stages.Any() ? project.Stages.Max(s => s.Sequence) + 1 : 1,
                Status = StageStatus.New,
                CompletionPercentage = 0,
                ActualCost = 0
            };
            _context.ProjectStages.Add(stage);
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
                            Status = ProjectTaskStatus.NotStarted,
                            Priority = TaskPriority.Medium,
                            CreatedAt = DateTime.UtcNow,
                            CreatedById = CurrentUserId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await _calc.RecalculateProjectAsync(projectId);

                TempData["Success"] = $"تم تفعيل مرحلة {template.Name} بنجاح";
                return RedirectToAction("Details", "Projects", new { id = projectId });
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

            await LoadDropdownsAsync(stage.ProjectId);
            return View(stage);
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
          [Bind("Name,Sequence,Status,AssignedEngineerId,DepartmentId,PlannedStartDate,PlannedEndDate,ActualStartDate,ActualEndDate,WorkDocumentation")] ProjectStage model)
        {
            var stage = await _context.ProjectStages.FindAsync(id);
            if (stage == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(stage.ProjectId);
                return View(model);
            }

            var wasCompleted = stage.Status == StageStatus.Completed;

            stage.Name = model.Name;
            stage.Sequence = model.Sequence;
            stage.Status = model.Status;
          
            stage.AssignedEngineerId = model.AssignedEngineerId;
            stage.DepartmentId = model.DepartmentId;
            stage.PlannedStartDate = model.PlannedStartDate;
            stage.PlannedEndDate = model.PlannedEndDate;
            stage.ActualStartDate = model.ActualStartDate;
            stage.ActualEndDate = model.ActualEndDate;
            stage.WorkDocumentation = model.WorkDocumentation;

            await _context.SaveChangesAsync();
            await _calc.RecalculateProjectAsync(stage.ProjectId);

            // إشعار اكتمال المرحلة (الإدارة + فريق المشروع) - القسم 10 بند 3
            if (!wasCompleted && stage.Status == StageStatus.Completed)
            {
                var teamIds = await _context.ProjectTeamMembers
                    .Where(tm => tm.ProjectId == stage.ProjectId)
                    .Select(tm => tm.UserId)
                    .ToListAsync();
                var adminIds = (await _userManager.GetUsersInRoleAsync("مدير النظام")).Select(u => u.Id);
                var recipients = teamIds.Union(adminIds).Distinct();

                await _notify.NotifyManyAsync(recipients, $"اكتملت المرحلة: {stage.Name}", $"/Projects/Details/{stage.ProjectId}");
            }

            TempData["Success"] = $"تم تحديث المرحلة {stage.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
        }

        // ============================================
        // حذف مرحلة (يحذف خطواتها ومهامها تلقائياً عبر Cascade)
        // ============================================
        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var stage = await _context.ProjectStages.FindAsync(id);
            if (stage == null)
                return NotFound();

            var projectId = stage.ProjectId;

            _context.ProjectStages.Remove(stage);
            await _context.SaveChangesAsync();

            await _calc.RecalculateProjectAsync(projectId);

            TempData["Success"] = "تم حذف المرحلة بنجاح";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        // ============================================
        // إنشاء خطوة داخل مرحلة
        // ============================================
        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStep(
            [Bind("StageId,Name,Weight,ActualCost")] ProjectStep model)
        {
            var stage = await _context.ProjectStages.Include(s => s.Steps).FirstOrDefaultAsync(s => s.Id == model.StageId);
            if (stage == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات الخطوة غير صحيحة";
                return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
            }

            var currentTotal = stage.Steps.Sum(s => s.Weight);
            if (currentTotal + model.Weight > 100)
            {
                TempData["Error"] = $"مجموع أوزان الخطوات سيتجاوز 100% (المجموع الحالي: {currentTotal}%)";
                return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
            }

            model.Status = StepStatus.NotStarted;
            _context.ProjectSteps.Add(model);
            await _context.SaveChangesAsync();

            await _calc.RecalculateStageAsync(stage.Id);

            TempData["Success"] = $"تمت إضافة الخطوة {model.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
        }

        // ============================================
        // تعديل خطوة (الوزن غير قابل للتعديل بعد الإنشاء)
        // اكتمال الخطوة يُسجَّل تاريخه ومُكمِلها تلقائياً
        // ============================================
        [RequirePermission("Projects.Stages.Manage")]
        [HttpGet]
        public async Task<IActionResult> EditStep(int id)
        {
            var step = await _context.ProjectSteps.Include(s => s.Stage).FirstOrDefaultAsync(s => s.Id == id);
            if (step == null)
                return NotFound();

            return View(step);
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStep(
            int id,
            [Bind("Name,Status,ActualCost")] ProjectStep model)
        {
            var step = await _context.ProjectSteps.Include(s => s.Stage).FirstOrDefaultAsync(s => s.Id == id);
            if (step == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var wasCompleted = step.Status == StepStatus.Completed;

            step.Name = model.Name;
            step.Status = model.Status;
            step.ActualCost = model.ActualCost;

            if (!wasCompleted && step.Status == StepStatus.Completed)
            {
                step.CompletedDate = DateTime.UtcNow;
                step.CompletedById = CurrentUserId;
            }
            else if (step.Status != StepStatus.Completed)
            {
                step.CompletedDate = null;
                step.CompletedById = null;
            }

            await _context.SaveChangesAsync();
            await _calc.RecalculateStageAsync(step.StageId);

            TempData["Success"] = $"تم تحديث الخطوة {step.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = step.Stage.ProjectId });
        }

        // ============================================
        // حذف خطوة
        // ============================================
        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStep(int id)
        {
            var step = await _context.ProjectSteps.Include(s => s.Stage).FirstOrDefaultAsync(s => s.Id == id);
            if (step == null)
                return NotFound();

            var stageId = step.StageId;
            var projectId = step.Stage.ProjectId;

            _context.ProjectSteps.Remove(step);
            await _context.SaveChangesAsync();

            await _calc.RecalculateStageAsync(stageId);

            TempData["Success"] = "تم حذف الخطوة بنجاح";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        private async Task LoadDropdownsAsync(int projectId)
        {
            ViewBag.Engineers = await _userManager.Users
.Where(u => u.IsActive)
.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
.ToListAsync();
            ViewBag.Departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        }
    }
}