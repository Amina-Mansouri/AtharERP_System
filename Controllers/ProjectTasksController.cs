using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    public class ProjectTasksController : Controller
    {
        private readonly AppDbContext _context;

        public ProjectTasksController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================
        // إنشاء مهمة داخل مرحلة
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProjectStageId,Title,Description,AssignedToId,DueDate,Priority,BonusAmount,PenaltyAmount")] ProjectTask model)
        {
            var stage = await _context.ProjectStages.FindAsync(model.ProjectStageId);
            if (stage == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات المهمة غير صحيحة";
                return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
            }

            model.Status = ProjectTaskStatus.NotStarted;
            model.CompletionPercentage = 0;

            _context.ProjectTasks.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تمت إضافة المهمة {model.Title} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
        }

        // ============================================
        // تعديل مهمة (تشمل عرض وإدارة التبعيات)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.ProjectStage)
                .Include(t => t.Dependencies).ThenInclude(d => d.DependsOnTask)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            // كل مهام نفس المشروع كخيارات للتبعية (باستثناء المهمة نفسها والتبعيات الحالية)
            var existingDependencyIds = task.Dependencies.Select(d => d.DependsOnTaskId).ToList();
            ViewBag.AvailableTasksForDependency = await _context.ProjectTasks
                .Include(t => t.ProjectStage)
                .Where(t => t.ProjectStage.ProjectId == task.ProjectStage.ProjectId
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
            [Bind("Title,Description,AssignedToId,DueDate,Priority,BonusAmount,PenaltyAmount,CompletionPercentage")] ProjectTask model)
        {
            var task = await _context.ProjectTasks.Include(t => t.ProjectStage).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            task.Title = model.Title;
            task.Description = model.Description;
            task.AssignedToId = model.AssignedToId;
            task.DueDate = model.DueDate;
            task.Priority = model.Priority;
            task.BonusAmount = model.BonusAmount;
            task.PenaltyAmount = model.PenaltyAmount;
            task.CompletionPercentage = model.CompletionPercentage;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم تحديث المهمة {task.Title} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = task.ProjectStage.ProjectId });
        }

        // ============================================
        // حذف مهمة (يحذف روابط التبعية أولاً لأن علاقتها Restrict)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.ProjectTasks.Include(t => t.ProjectStage).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return NotFound();

            var projectId = task.ProjectStage.ProjectId;

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
        // تحديث حالة المهمة (لوحة Kanban) - يمنع بدء المهمة قبل اكتمال تبعياتها
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ProjectTaskStatus status)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.Dependencies).ThenInclude(d => d.DependsOnTask)
                .Include(t => t.ProjectStage)
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
                    return RedirectToAction("Details", "Projects", new { id = task.ProjectStage.ProjectId });
                }
            }

            task.Status = status;
            if (status == ProjectTaskStatus.Completed)
                task.CompletionPercentage = 100;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم تحديث حالة المهمة {task.Title}";
            return RedirectToAction("Details", "Projects", new { id = task.ProjectStage.ProjectId });
        }

        // ============================================
        // إضافة تبعية بين مهمتين
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDependency(int taskId, int dependsOnTaskId)
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

            _context.TaskDependencies.Add(new TaskDependency { TaskId = taskId, DependsOnTaskId = dependsOnTaskId });
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