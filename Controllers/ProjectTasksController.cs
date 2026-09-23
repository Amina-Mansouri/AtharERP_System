using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class ProjectTasksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ProjectCalculationService _calc;
        private readonly NotificationService _notify;
        private readonly PermissionService _permissionService;
        private readonly FileUploadService _fileUpload;
        private readonly ProposalReviewPdfService _pdfService;

        public ProjectTasksController(
            AppDbContext context,
            ProjectCalculationService calc,
            NotificationService notify,
            PermissionService permissionService,
            FileUploadService fileUpload,
            ProposalReviewPdfService pdfService)
        {
            _context = context;
            _calc = calc;
            _notify = notify;
            _permissionService = permissionService;
            _fileUpload = fileUpload;
            _pdfService = pdfService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task<bool> CanExecuteAsync(ProjectTask task)
        {
            if (await _permissionService.HasPermissionAsync(User, "Projects.Tasks.Manage"))
                return true;
           
            if (task.ProjectAssignmentId.HasValue)
            {
                if (await _context.AssignmentEngineers.AnyAsync(e => e.ProjectAssignmentId == task.ProjectAssignmentId.Value && e.UserId == CurrentUserId))
                    return true;
            }
            var stageEngineerId = await _context.ProjectStages
                .Where(s => s.Id == task.StageId)
                .Select(s => s.AssignedEngineerId)
                .FirstOrDefaultAsync();
            return stageEngineerId == CurrentUserId;
        }

        // تعديل تواريخ المهمة (بداية/نهاية/تسليم فعلي) — للمدير أو مسؤول المرحلة فقط، وليس أي مكلَّف
        private async Task<bool> CanEditDatesAsync(ProjectTask task)
        {
            if (await _permissionService.HasPermissionAsync(User, "Projects.Tasks.Manage"))
                return true;
            var stageEngineerId = await _context.ProjectStages
                .Where(s => s.Id == task.StageId)
                .Select(s => s.AssignedEngineerId)
                .FirstOrDefaultAsync();
            return stageEngineerId == CurrentUserId;
        }

        private async Task<bool> IsTaskWorkerAsync(ProjectTask task)
        {
          
            if (task.ProjectAssignmentId.HasValue)
            {
                if (await _context.AssignmentEngineers.AnyAsync(e => e.ProjectAssignmentId == task.ProjectAssignmentId.Value && e.UserId == CurrentUserId))
                    return true;
            }
            var stageEngineerId = await _context.ProjectStages
                .Where(s => s.Id == task.StageId)
                .Select(s => s.AssignedEngineerId)
                .FirstOrDefaultAsync();
            return stageEngineerId == CurrentUserId;
        }

        private async Task<bool> IsProjectLockedAsync(int projectId)
        {
            var status = await _context.Projects.Where(p => p.Id == projectId).Select(p => p.Status).FirstOrDefaultAsync();
            return status == ProjectStatus.OnHold || status == ProjectStatus.Cancelled;
        }

        private async Task<bool> IsAssignmentLockedAsync(ProjectTask task)
        {
            if (!task.ProjectAssignmentId.HasValue)
                return false;
            var status = await _context.ProjectAssignments
                .Where(a => a.Id == task.ProjectAssignmentId.Value)
                .Select(a => a.Status)
                .FirstOrDefaultAsync();
            return status == AssignmentStatus.Cancelled;
        }

        // ============================================
        // مهامي (كل المهام المكلَّف بها المستخدم الحالي عبر أي مشروع)
        // ============================================
        [Authorize]
      
        public async Task<IActionResult> MyTasks()
        {
            var myAssignmentIds = await _context.AssignmentEngineers
                .Where(e => e.UserId == CurrentUserId)
                .Select(e => e.ProjectAssignmentId)
                .ToListAsync();

            var tasks = await _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.Stage)
                .Include(t => t.Todos)
                .Where(t => (t.ProjectAssignmentId.HasValue && myAssignmentIds.Contains(t.ProjectAssignmentId.Value))
                         || (t.Stage != null && t.Stage.AssignedEngineerId == CurrentUserId))
                .OrderBy(t => t.Status)
                .ThenBy(t => t.PlannedEndDate)
                .ToListAsync();

            return View(tasks);
        }

        private static bool IsTaskFrozen(ProjectTask t)
        {
            return t.Status == ProjectTaskStatus.Blocked
               || (t.ProjectAssignment != null && t.ProjectAssignment.Status == AssignmentStatus.Cancelled);
        }

        // ============================================
        // إنشاء مهمة داخل مرحلة
        // ============================================
        [RequirePermission("Projects.Tasks.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
        [Bind("StageId,ProjectAssignmentId,Title,Description,PlannedStartDate,PlannedEndDate,Priority,IsUrgent,Weight")] ProjectTask model)
        {
            var stage = await _context.ProjectStages.Include(s => s.Tasks).ThenInclude(t => t.ProjectAssignment).FirstOrDefaultAsync(s => s.Id == model.StageId);
            if (stage == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات المهمة غير صحيحة";
                return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
            }

            var otherTasksWeightTotal = stage.Tasks.Where(t => !IsTaskFrozen(t)).Sum(t => t.Weight);
            if (otherTasksWeightTotal + model.Weight > stage.Weight)
            {
                TempData["Error"] = $"سيتجاوز مجموع أوزان مهام مرحلة \"{stage.Name}\" وزنها ({stage.Weight:N0}%)";
                return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
            }

            model.ProjectId = stage.ProjectId;
            model.Status = ProjectTaskStatus.NotStarted;
            model.CompletionPercentage = 0;
            model.DelayDays = 0;
            model.EarlyDeliveryDays = 0;
            model.CreatedAt = DateTime.UtcNow;
            model.CreatedById = CurrentUserId;

            _context.ProjectTasks.Add(model);
            await _context.SaveChangesAsync();
            await _calc.RecalculateStageAsync(stage.Id);

            TempData["Success"] = $"تمت إضافة المهمة {model.Title} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
        }
        // ============================================
        // عرض/تعديل مهمة - العرض متاح للمكلَّف بالمهمة أيضاً، والحفظ (تعديل البيانات) للإدارة فقط
        // ============================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.ProjectTasks
 
     .Include(t => t.Todos)
     .Include(t => t.Dependencies).ThenInclude(d => d.DependsOnTask)
     .Include(t => t.Stage).ThenInclude(s => s.Project)
     .Include(t => t.ProjectAssignment).ThenInclude(a => a!.Engineers).ThenInclude(e => e.User)
     .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            if (!await CanExecuteAsync(task))
                return Forbid();

            var canManage = await _permissionService.HasPermissionAsync(User, "Projects.Tasks.Manage");
            ViewBag.CanManage = canManage;
            ViewBag.CanEditDates = canManage || await CanEditDatesAsync(task);
            ViewBag.Proposals = await _context.DesignProposals
    .Include(p => p.PreparedBy)
    .Where(p => p.ProjectTaskId == id)
    .OrderByDescending(p => p.CreatedAt)
    .ToListAsync();

            if (canManage)
            {
                ViewBag.Engineers = await _context.ProjectTeamMembers
    .Where(tm => tm.ProjectId == task.ProjectId)
    .Select(tm => tm.User)
    .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
    .ToListAsync();
                ViewBag.Assignments = await _context.ProjectAssignments
    .Where(a => a.StageId == task.StageId)
    .ToListAsync();
                var existingDependencyIds = task.Dependencies.Select(d => d.DependsOnTaskId).ToList();
                ViewBag.AvailableTasksForDependency = await _context.ProjectTasks
                    .Where(t => t.ProjectId == task.ProjectId && t.Id != task.Id && !existingDependencyIds.Contains(t.Id))
                    .ToListAsync();
            }

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    int id,
    [Bind("Title,Description,PlannedStartDate,PlannedEndDate,ActualDeliveryDate,Priority,IsUrgent,Weight")] ProjectTask model)
        {
            var task = await _context.ProjectTasks.Include(t => t.Todos).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return NotFound();

            var canManage = await _permissionService.HasPermissionAsync(User, "Projects.Tasks.Manage");
            var canEditDates = canManage || await CanEditDatesAsync(task);

            if (!canManage && !canEditDates)
                return Forbid();

            if (await IsProjectLockedAsync(task.ProjectId))
            {
                TempData["Error"] = "المشروع متوقف أو ملغى — لا يمكن تعديل مهامه حالياً";
                return this.RedirectKeepingTab("Edit", new { id });
            }
            if (await IsAssignmentLockedAsync(task))
            {
                TempData["Error"] = "التكليف معلَّق أو ملغى — لا يمكن التعامل مع مهامه حالياً";
                return this.RedirectKeepingTab("Edit", new { id });
            }
            if (!ModelState.IsValid)
                return View(model);

            if (task.Status == ProjectTaskStatus.Completed)
            {
                TempData["Error"] = "لا يمكن تعديل بيانات مهمة مكتملة";
                return this.RedirectKeepingTab("Edit", new { id });
            }

            if (canManage)
            {
                var stage = await _context.ProjectStages.Include(s => s.Tasks).ThenInclude(t => t.ProjectAssignment).FirstOrDefaultAsync(s => s.Id == task.StageId);
                var otherTasksWeightTotal = stage!.Tasks.Where(t => t.Id != id && !IsTaskFrozen(t)).Sum(t => t.Weight);
               
                if (otherTasksWeightTotal + model.Weight > stage.Weight)
                {
                    TempData["Error"] = $"سيتجاوز مجموع أوزان مهام مرحلة \"{stage.Name}\" وزنها ({stage.Weight:N0}%)";
                    return this.RedirectKeepingTab("Edit", new { id });
                }

                task.Title = model.Title;
                task.Description = model.Description;
                task.Priority = model.Priority;
                task.IsUrgent = model.IsUrgent;
                task.Weight = model.Weight;
            }

            if (canEditDates)
            {
                var assignmentForDates = task.ProjectAssignmentId.HasValue
                    ? await _context.ProjectAssignments.FindAsync(task.ProjectAssignmentId.Value)
                    : null;

                DateTime? rangeStart = assignmentForDates?.PlannedStartDate;
                DateTime? rangeEnd = assignmentForDates?.PlannedEndDate;
                var rangeSource = "التكليف";

                if (rangeStart == null || rangeEnd == null)
                {
                    var stageForDates = await _context.ProjectStages.FindAsync(task.StageId);
                    rangeStart = stageForDates?.PlannedStartDate;
                    rangeEnd = stageForDates?.PlannedEndDate;
                    rangeSource = "المرحلة";
                }

                if (rangeStart != null && rangeEnd != null)
                {
                    if ((model.PlannedStartDate.HasValue && model.PlannedStartDate < rangeStart) ||
                        (model.PlannedEndDate.HasValue && model.PlannedEndDate > rangeEnd))
                    {
                        TempData["Error"] = $"تواريخ المهمة يجب أن تكون ضمن نطاق {rangeSource} ({rangeStart:yyyy-MM-dd} إلى {rangeEnd:yyyy-MM-dd})";
                        return this.RedirectKeepingTab("Edit", new { id });
                    }
                }

                task.PlannedStartDate = model.PlannedStartDate;
                task.PlannedEndDate = model.PlannedEndDate;
                task.ActualDeliveryDate = model.ActualDeliveryDate;

                _calc.UpdateDeliveryMetrics(task);

                if (model.ActualDeliveryDate.HasValue)
                {
                    foreach (var todo in task.Todos.Where(t => !t.IsCompleted))
                    {
                        todo.IsCompleted = true;
                        todo.CompletedAt = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (canEditDates && model.ActualDeliveryDate.HasValue)
            {
                await _calc.RecalculateTaskCompletionAsync(id);
            }

            if (canManage)
            {
                await _calc.RecalculateStageAsync(task.StageId!.Value);
            }

            if (task.DelayDays > 0)
            {
                var recipientIds = await _permissionService.GetProjectRecipientsAsync(task.ProjectId);
                await _notify.NotifyManyAsync(recipientIds, $"المهمة \"{task.Title}\" متأخرة بمقدار {task.DelayDays} يوم", NotificationEventType.TaskDelayed, $"/ProjectTasks/Edit/{task.Id}", requiresAction: true, entityType: "ProjectTask", entityId: task.Id);
            }

            TempData["Success"] = $"تم تحديث المهمة {task.Title} بنجاح";
            return this.RedirectKeepingTab("Edit", new { id });

        }
        // ============================================
        // حذف مهمة
        // ============================================
        [RequirePermission("Projects.Tasks.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return NotFound();

            var projectId = task.ProjectId;

            var dependencyLinks = await _context.TaskDependencies
                .Where(d => d.TaskId == id || d.DependsOnTaskId == id)
                .ToListAsync();
            _context.TaskDependencies.RemoveRange(dependencyLinks);

            _context.ProjectTasks.Remove(task);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف المهمة بنجاح";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        // ============================================
        // تحديث حالة المهمة - مسموح للمدير أو للمكلَّف بالمهمة نفسها فقط
        // ============================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlocked(int id)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return NotFound();

            if (!await _permissionService.HasPermissionAsync(User, "Projects.Tasks.Manage"))
                return Forbid();

            if (await IsProjectLockedAsync(task.ProjectId))
            {
                TempData["Error"] = "المشروع متوقف أو ملغى — لا يمكن التعامل مع مهامه حالياً";
                return this.RedirectKeepingTab("Edit", new { id });
            }

            task.Status = task.Status == ProjectTaskStatus.Blocked
                ? (task.CompletionPercentage >= 100 ? ProjectTaskStatus.PendingReview : task.CompletionPercentage > 0 ? ProjectTaskStatus.InProgress : ProjectTaskStatus.NotStarted)
                : ProjectTaskStatus.Blocked;

            await _context.SaveChangesAsync();
            await _calc.RecalculateStageAsync(task.StageId!.Value);

            TempData["Success"] = "تم تحديث حالة المهمة";
            return this.RedirectKeepingTab("Edit", new { id });
        }

     
        // ============================================
        // قائمة To-Do - مسموح للمكلَّف بالمهمة نفسها فقط
        // ============================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTodo(int taskId, string item)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            if (!await IsTaskWorkerAsync(task))
                return Forbid();

            if (await IsProjectLockedAsync(task.ProjectId))
            {
                TempData["Error"] = "المشروع متوقف أو ملغى — لا يمكن التعامل مع مهامه حالياً";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }
            if (await IsAssignmentLockedAsync(task))
            {
                TempData["Error"] = "التكليف معلَّق أو ملغى — لا يمكن التعامل مع مهامه حالياً";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }
            if (task.Status == ProjectTaskStatus.Completed || task.Status == ProjectTaskStatus.PendingReview)
            {
                TempData["Error"] = "لا يمكن إضافة بند لمهمة مكتملة أو قيد المراجعة";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }

            if (task.PlannedStartDate == null || task.PlannedEndDate == null)
            {
                TempData["Error"] = "لا يمكن إضافة بند قبل تحديد تاريخ البداية والنهاية للمهمة";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }

            if (!string.IsNullOrWhiteSpace(item))
            {
                _context.TaskTodos.Add(new TaskTodo { TaskId = taskId, Item = item });
                await _context.SaveChangesAsync();
                await _calc.RecalculateTaskCompletionAsync(taskId);
                if (task.ProjectAssignmentId.HasValue)
                {
                    await _calc.MarkAssignmentInProgressAsync(task.ProjectAssignmentId.Value);
                }
            }

            return this.RedirectKeepingTab("Edit", new { id = taskId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTodo(int id, int taskId)
        {
            var task = await _context.ProjectTasks.Include(t => t.Todos).FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            if (!await IsTaskWorkerAsync(task))
                return Forbid();

            if (await IsProjectLockedAsync(task.ProjectId))
            {
                TempData["Error"] = "المشروع متوقف أو ملغى — لا يمكن التعامل مع مهامه حالياً";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }
            if (await IsAssignmentLockedAsync(task))
            {
                TempData["Error"] = "التكليف معلَّق أو ملغى — لا يمكن التعامل مع مهامه حالياً";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }
            if (task.Status == ProjectTaskStatus.Completed || task.Status == ProjectTaskStatus.PendingReview)
            {
                TempData["Error"] = "لا يمكن تعديل بنود مهمة مكتملة أو قيد المراجعة";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }

            var todo = task.Todos.FirstOrDefault(t => t.Id == id);
            if (todo != null)
            {
                todo.IsCompleted = !todo.IsCompleted;
                todo.CompletedAt = todo.IsCompleted ? DateTime.UtcNow : null;

                await _context.SaveChangesAsync();
                await _calc.RecalculateTaskCompletionAsync(taskId);
            }

            return this.RedirectKeepingTab("Edit", new { id = taskId });
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTodo(int id, int taskId)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            if (!await IsTaskWorkerAsync(task))
                return Forbid();

            if (await IsProjectLockedAsync(task.ProjectId))
            {
                TempData["Error"] = "المشروع متوقف أو ملغى — لا يمكن التعامل مع مهامه حالياً";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }
            if (await IsAssignmentLockedAsync(task))
            {
                TempData["Error"] = "التكليف معلَّق أو ملغى — لا يمكن التعامل مع مهامه حالياً";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }
            if (task.Status == ProjectTaskStatus.Completed || task.Status == ProjectTaskStatus.PendingReview)
            {
                TempData["Error"] = "لا يمكن تعديل بنود مهمة مكتملة أو قيد المراجعة";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }

            var todo = await _context.TaskTodos.FindAsync(id);
            if (todo != null)
            {
                _context.TaskTodos.Remove(todo);
                await _context.SaveChangesAsync();
                await _calc.RecalculateTaskCompletionAsync(taskId);
            }

            return this.RedirectKeepingTab("Edit", new { id = taskId });
        }

        // ============================================
        // إضافة/حذف تبعية بين مهمتين - إدارة فقط
        // ============================================
        [RequirePermission("Projects.Tasks.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDependency(int taskId, int dependsOnTaskId)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            var dependsOnTaskExists = await _context.ProjectTasks.AnyAsync(t => t.Id == dependsOnTaskId);
            if (!dependsOnTaskExists)
            {
                TempData["Error"] = "لم يتم اختيار مهمة صحيحة للاعتماد عليها";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }

            if (taskId == dependsOnTaskId)
            {
                TempData["Error"] = "لا يمكن أن تعتمد المهمة على نفسها";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }

            var alreadyExists = await _context.TaskDependencies
                .AnyAsync(d => d.TaskId == taskId && d.DependsOnTaskId == dependsOnTaskId);

            var reverseExists = await _context.TaskDependencies
                .AnyAsync(d => d.TaskId == dependsOnTaskId && d.DependsOnTaskId == taskId);

            if (alreadyExists)
            {
                TempData["Error"] = "رابط التبعية موجود بالفعل";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }

            if (reverseExists)
            {
                TempData["Error"] = "لا يمكن إنشاء تبعية دائرية بين هاتين المهمتين";
                return this.RedirectKeepingTab("Edit", new { id = taskId });
            }

            _context.TaskDependencies.Add(new TaskDependency { TaskId = taskId, DependsOnTaskId = dependsOnTaskId });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة التبعية بنجاح";
            return this.RedirectKeepingTab("Edit", new { id = taskId });
        }

        [RequirePermission("Projects.Tasks.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDependency(int id, int taskId)
        {
            var link = await _context.TaskDependencies.FindAsync(id);
            if (link != null)
            {
                _context.TaskDependencies.Remove(link);
                await _context.SaveChangesAsync();
            }

            return this.RedirectKeepingTab("Edit", new { id = taskId });
        }
        // ============================================
        // اعتماد/رفض مهمة قيد المراجعة — للمدير أو مسؤول المرحلة فقط
        // ============================================
        private async Task<List<string>> GetTaskWorkerIdsAsync(ProjectTask task)
        {
            if (!task.ProjectAssignmentId.HasValue)
                return new List<string>();

            return await _context.AssignmentEngineers
                .Where(e => e.ProjectAssignmentId == task.ProjectAssignmentId.Value)
                .Select(e => e.UserId)
                .Distinct()
                .ToListAsync();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ReviewTask(int id, int? projectId, int? stageId, string? taskFilter)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.Project).ThenInclude(p => p.ParentProject)
                .Include(t => t.Stage)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            if (!await CanEditDatesAsync(task))
                return Forbid();

            ViewBag.Task = task;
            ViewBag.CurrentUserName = User.FindFirstValue(ClaimTypes.Name) ?? "";
            ViewBag.ProjectId = projectId;
            ViewBag.StageId = stageId;
            ViewBag.TaskFilter = taskFilter;
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewTask(
            int id,
            ProposalStatus status,
            string reviewerName,
            string? reviewerPosition,
            IFormFile? reviewerSignature,
            string? supervisorName,
            string? supervisorPosition,
            IFormFile? supervisorSignature,
            string? notes,
            string? discipline,
            int? projectId,
            int? stageId,
            string? taskFilter)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.Todos)
                .Include(t => t.Project).ThenInclude(p => p.ParentProject)
                .Include(t => t.Stage)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            if (!await CanEditDatesAsync(task))
                return Forbid();
            if (await IsProjectLockedAsync(task.ProjectId))
            {
                TempData["Error"] = "المشروع متوقف أو ملغى — لا يمكن التعامل مع مهامه حالياً";
                return RedirectToAction("Overview", "ProjectAssignments", new { projectId, stageId, taskFilter });
            }
            if (task.Status != ProjectTaskStatus.PendingReview)
            {
                TempData["Error"] = "المهمة ليست قيد المراجعة";
                return RedirectToAction("Overview", "ProjectAssignments", new { projectId, stageId, taskFilter });
            }

            bool isApprovalOutcome = status == ProposalStatus.Approved || status == ProposalStatus.ApprovedWithModification;
            var workerIds = await GetTaskWorkerIdsAsync(task);

            if (isApprovalOutcome)
            {
                task.Status = ProjectTaskStatus.Completed;
                task.ReviewComment = notes;
                await _context.SaveChangesAsync();
                await _calc.RecalculateStageAsync(task.StageId!.Value);

                var message = string.IsNullOrWhiteSpace(notes)
                    ? $"تم اعتماد مهمتك \"{task.Title}\""
                    : $"تم اعتماد مهمتك \"{task.Title}\" — {notes}";
                foreach (var workerId in workerIds)
                {
                    await _notify.NotifyAsync(workerId, message, NotificationEventType.TaskStatusChanged, $"/ProjectTasks/Edit/{task.Id}", entityType: "ProjectTask", entityId: task.Id);
                }

                TempData["Success"] = "تم اعتماد المهمة";
            }
            else
            {
                foreach (var todo in task.Todos)
                {
                    todo.IsCompleted = false;
                    todo.CompletedAt = null;
                }

                task.CompletionPercentage = 0;
                task.Status = ProjectTaskStatus.InProgress;
                task.ReviewComment = notes;
                task.ActualDeliveryDate = null;
                task.RejectionCount++;
                _calc.UpdateDeliveryMetrics(task);
                await _context.SaveChangesAsync();
                await _calc.RecalculateStageAsync(task.StageId!.Value);

                var message = string.IsNullOrWhiteSpace(notes)
                    ? $"تم رفض مهمتك \"{task.Title}\" — تحتاج مراجعة"
                    : $"تم رفض مهمتك \"{task.Title}\" — {notes}";
                foreach (var workerId in workerIds)
                {
                    await _notify.NotifyAsync(workerId, message, NotificationEventType.TaskStatusChanged, $"/ProjectTasks/Edit/{task.Id}", requiresAction: true, entityType: "ProjectTask", entityId: task.Id);
                }

                TempData["Success"] = "تم رفض المهمة وإعادتها قيد التنفيذ";
            }

            var review = new ProposalReview
            {
                ProjectTaskId = task.Id,
                Status = status,
                ReviewerName = reviewerName,
                ReviewerPosition = reviewerPosition,
                SupervisorName = supervisorName,
                SupervisorPosition = supervisorPosition,
                Notes = notes,
                Discipline = discipline,
                ReviewDate = DateTime.UtcNow,
                ReviewedById = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };

            if (reviewerSignature != null && reviewerSignature.Length > 0)
            {
                var sigResult = await _fileUpload.SaveFileAsync(reviewerSignature, $"reviews/task-{task.Id}");
                if (sigResult.Success)
                    review.ReviewerSignaturePath = sigResult.FilePath;
            }

            if (supervisorSignature != null && supervisorSignature.Length > 0)
            {
                var sigResult = await _fileUpload.SaveFileAsync(supervisorSignature, $"reviews/task-{task.Id}");
                if (sigResult.Success)
                    review.SupervisorSignaturePath = sigResult.FilePath;
            }

            _context.ProposalReviews.Add(review);
            await _context.SaveChangesAsync();

            var subProject = task.Project.Scope == ProjectScope.Sub ? task.Project : null;
            var mainProject = subProject != null && task.Project.ParentProject != null ? task.Project.ParentProject : task.Project;

            var pdfBytes = _pdfService.Generate(review, mainProject, subProject, task.Stage?.Name, task.Title, null);
            var pdfResult = await _fileUpload.SaveGeneratedFileAsync(pdfBytes, $"reviews/task-{task.Id}", ".pdf");
            if (pdfResult.Success)
            {
                review.PdfFilePath = pdfResult.FilePath;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Overview", "ProjectAssignments", new { projectId, stageId, taskFilter });
        }
    }
}