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
    public class ProjectTasksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ProjectCalculationService _calc;
        private readonly AuditService _audit;
        private readonly NotificationService _notify;

        public ProjectTasksController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ProjectCalculationService calc,
            AuditService audit,
            NotificationService notify)
        {
            _context = context;
            _userManager = userManager;
            _calc = calc;
            _audit = audit;
            _notify = notify;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // إنشاء مهمة داخل مرحلة (مع مكلَّف رئيسي اختياري - نسبة مساهمة 100%)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("StageId,Title,Description,DueDate,Priority,IsUrgent,BonusAmount,PenaltyAmount")] ProjectTask model,
            string? assigneeId)
        {
            var stage = await _context.ProjectStages.FindAsync(model.StageId);
            if (stage == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات المهمة غير صحيحة";
                return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
            }

            model.ProjectId = stage.ProjectId;
            model.Status = ProjectTaskStatus.NotStarted;
            model.CompletionPercentage = 0;
            model.CreatedAt = DateTime.UtcNow;
            model.CreatedById = CurrentUserId;

            _context.ProjectTasks.Add(model);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(assigneeId))
            {
                _context.TaskAssignees.Add(new TaskAssignee
                {
                    TaskId = model.Id,
                    UserId = assigneeId,
                    ContributionPercentage = 100,
                    AssignedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                // إشعار تلقائي عند تكليف المهندس بمهمة (القسم 6.3.2)
                await _notify.NotifyAsync(assigneeId, $"تم تكليفك بمهمة جديدة: {model.Title}", $"/Projects/Details/{stage.ProjectId}");
            }

            await _audit.LogAsync(CurrentUserId, "Create", nameof(ProjectTask), model.Id.ToString(), $"إضافة مهمة: {model.Title}");

            TempData["Success"] = $"تمت إضافة المهمة {model.Title} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
        }

        // ============================================
        // تعديل مهمة (تشمل عرض وإدارة المكلَّفين، البنود، والتبعيات)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.Stage)
                .Include(t => t.Assignees).ThenInclude(a => a.User)
                .Include(t => t.Todos)
                .Include(t => t.Dependencies).ThenInclude(d => d.DependsOnTask)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            ViewBag.Engineers = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            // كل مهام نفس المشروع كخيارات للتبعية (باستثناء المهمة نفسها والتبعيات الحالية)
            var existingDependencyIds = task.Dependencies.Select(d => d.DependsOnTaskId).ToList();
            ViewBag.AvailableTasksForDependency = await _context.ProjectTasks
                .Where(t => t.ProjectId == task.ProjectId
                         && t.Id != task.Id
                         && !existingDependencyIds.Contains(t.Id))
                .ToListAsync();

            return View(task);
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Title,Description,DueDate,PlannedStartDate,PlannedEndDate,ActualDeliveryDate,Priority,IsUrgent,BonusAmount,PenaltyAmount")] ProjectTask model)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            task.Title = model.Title;
            task.Description = model.Description;
            task.DueDate = model.DueDate;
            task.PlannedStartDate = model.PlannedStartDate;
            task.PlannedEndDate = model.PlannedEndDate;
            task.ActualDeliveryDate = model.ActualDeliveryDate;
            task.Priority = model.Priority;
            task.IsUrgent = model.IsUrgent;
            task.BonusAmount = model.BonusAmount;
            task.PenaltyAmount = model.PenaltyAmount;

            // حساب أيام التأخير/التبكير تلقائياً عند تسجيل تاريخ التسليم الفعلي (القسم 5.4/5.5)
            _calc.UpdateDeliveryMetrics(task);

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ProjectTask), task.Id.ToString(), $"تعديل مهمة: {task.Title}");

            TempData["Success"] = $"تم تحديث المهمة {task.Title} بنجاح";
            return RedirectToAction("Edit", new { id });
        }

        // ============================================
        // حذف مهمة (يحذف روابط التبعية أولاً لأن علاقتها Restrict)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return NotFound();

            var projectId = task.ProjectId;
            var taskTitle = task.Title;

            var dependencyLinks = await _context.TaskDependencies
                .Where(d => d.TaskId == id || d.DependsOnTaskId == id)
                .ToListAsync();
            _context.TaskDependencies.RemoveRange(dependencyLinks);

            _context.ProjectTasks.Remove(task);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(ProjectTask), id.ToString(), $"حذف مهمة: {taskTitle}");

            TempData["Success"] = "تم حذف المهمة بنجاح";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        // ============================================
        // تحديث حالة المهمة (لوحة Kanban) - يمنع بدء المهمة قبل اكتمال تبعياتها
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ProjectTaskStatus status)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.Dependencies).ThenInclude(d => d.DependsOnTask)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            if (status == ProjectTaskStatus.InProgress)
            {
                var incompleteDependencies = task.Dependencies
                    .Where(d => d.DependsOnTask.Status != ProjectTaskStatus.Completed)
                    .Select(d => d.DependsOnTask.Title)
                    .ToList();

                if (incompleteDependencies.Any())
                {
                    TempData["Error"] = $"لا يمكن بدء هذه المهمة قبل إكمال: {string.Join("، ", incompleteDependencies)}";
                    return RedirectToAction("Details", "Projects", new { id = task.ProjectId });
                }
            }

            task.Status = status;
            if (status == ProjectTaskStatus.Completed)
            {
                task.CompletionPercentage = 100;
                if (task.ActualDeliveryDate == null)
                    task.ActualDeliveryDate = DateTime.UtcNow.Date;
                _calc.UpdateDeliveryMetrics(task);
            }

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "UpdateStatus", nameof(ProjectTask), task.Id.ToString(), $"{task.Title} -> {status}");

            TempData["Success"] = $"تم تحديث حالة المهمة {task.Title}";
            return RedirectToAction("Details", "Projects", new { id = task.ProjectId });
        }

        // ============================================
        // المكلَّفون بالمهمة (تكليف عدة مهندسين بنسب مساهمة - القسم 3.6/6.3.1)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAssignee(int taskId, string userId, decimal contributionPercentage)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "الرجاء اختيار مهندس";
                return RedirectToAction("Edit", new { id = taskId });
            }

            var exists = await _context.TaskAssignees.AnyAsync(a => a.TaskId == taskId && a.UserId == userId);
            if (exists)
            {
                TempData["Error"] = "هذا المهندس مكلَّف بالفعل بهذه المهمة";
                return RedirectToAction("Edit", new { id = taskId });
            }

            _context.TaskAssignees.Add(new TaskAssignee
            {
                TaskId = taskId,
                UserId = userId,
                ContributionPercentage = contributionPercentage <= 0 ? 100 : contributionPercentage,
                AssignedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            await _notify.NotifyAsync(userId, $"تم تكليفك بمهمة: {task.Title}", $"/Projects/Details/{task.ProjectId}");

            TempData["Success"] = "تمت إضافة المكلَّف بنجاح";
            return RedirectToAction("Edit", new { id = taskId });
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAssignee(int id, int taskId)
        {
            var assignee = await _context.TaskAssignees.FindAsync(id);
            if (assignee != null)
            {
                _context.TaskAssignees.Remove(assignee);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Edit", new { id = taskId });
        }

        // ============================================
        // قائمة To-Do داخل المهمة (نسبة الإنجاز تُحسب منها تلقائياً - القسم 5.6)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTodo(int taskId, string item)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(item))
            {
                _context.TaskTodos.Add(new TaskTodo { TaskId = taskId, Item = item, IsCompleted = false });
                await _context.SaveChangesAsync();
                await _calc.RecalculateTaskCompletionAsync(taskId);
            }

            return RedirectToAction("Edit", new { id = taskId });
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTodo(int id, int taskId)
        {
            var todo = await _context.TaskTodos.FindAsync(id);
            if (todo != null)
            {
                todo.IsCompleted = !todo.IsCompleted;
                todo.CompletedAt = todo.IsCompleted ? DateTime.UtcNow : null;
                await _context.SaveChangesAsync();
                await _calc.RecalculateTaskCompletionAsync(taskId);
            }

            return RedirectToAction("Edit", new { id = taskId });
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTodo(int id, int taskId)
        {
            var todo = await _context.TaskTodos.FindAsync(id);
            if (todo != null)
            {
                _context.TaskTodos.Remove(todo);
                await _context.SaveChangesAsync();
                await _calc.RecalculateTaskCompletionAsync(taskId);
            }

            return RedirectToAction("Edit", new { id = taskId });
        }

        // ============================================
        // إضافة تبعية بين مهمتين (مع نوع التبعية - القسم 3.8)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDependency(int taskId, int dependsOnTaskId, DependencyType type)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            if (taskId == dependsOnTaskId)
            {
                TempData["Error"] = "لا يمكن أن تعتمد المهمة على نفسها";
                return RedirectToAction("Edit", new { id = taskId });
            }

            var alreadyExists = await _context.TaskDependencies
                .AnyAsync(d => d.TaskId == taskId && d.DependsOnTaskId == dependsOnTaskId);

            var reverseExists = await _context.TaskDependencies
                .AnyAsync(d => d.TaskId == dependsOnTaskId && d.DependsOnTaskId == taskId);

            if (alreadyExists)
            {
                TempData["Error"] = "رابط التبعية موجود بالفعل";
                return RedirectToAction("Edit", new { id = taskId });
            }

            if (reverseExists)
            {
                TempData["Error"] = "لا يمكن إنشاء تبعية دائرية بين هاتين المهمتين";
                return RedirectToAction("Edit", new { id = taskId });
            }

            _context.TaskDependencies.Add(new TaskDependency { TaskId = taskId, DependsOnTaskId = dependsOnTaskId, Type = type });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة التبعية بنجاح";
            return RedirectToAction("Edit", new { id = taskId });
        }

        // ============================================
        // حذف تبعية
        // ============================================
        [RequirePermission("Projects.Edit")]
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

            return RedirectToAction("Edit", new { id = taskId });
        }
    }
}