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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ProjectCalculationService _calc;
        private readonly AuditService _audit;

        public ProjectStagesController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ProjectCalculationService calc,
            AuditService audit)
        {
            _context = context;
            _userManager = userManager;
            _calc = calc;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // إنشاء مرحلة جديدة (نموذج مضمّن داخل صفحة تفاصيل المشروع)
        // مجموع أوزان مراحل المشروع يجب ألا يتجاوز 100% (القسم 6.1)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProjectId,Name,Sequence,Weight,Cost,AssignedEngineerId,DepartmentId,PlannedStartDate,PlannedEndDate")] ProjectStage model)
        {
            var project = await _context.Projects.Include(p => p.Stages).FirstOrDefaultAsync(p => p.Id == model.ProjectId);
            if (project == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات المرحلة غير صحيحة";
                return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
            }

            if (string.IsNullOrEmpty(model.AssignedEngineerId))
                model.AssignedEngineerId = null;

            var currentTotal = project.Stages.Sum(s => s.Weight);
            if (currentTotal + model.Weight > 100)
            {
                TempData["Error"] = $"مجموع أوزان المراحل سيتجاوز 100% (المجموع الحالي: {currentTotal}%)";
                return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
            }

            model.Status = StageStatus.New;
            model.CompletionPercentage = 0;

            _context.ProjectStages.Add(model);
            await _context.SaveChangesAsync();

            await _calc.RecalculateProjectAsync(model.ProjectId);

            await _audit.LogAsync(CurrentUserId, "Create", nameof(ProjectStage), model.Id.ToString(), $"إضافة مرحلة: {model.Name} (مشروع {project.Code})");

            TempData["Success"] = $"تمت إضافة المرحلة {model.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
        }

        // ============================================
        // تعديل مرحلة (لا يمكن تعديل الوزن بعد الإنشاء)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var stage = await _context.ProjectStages.FindAsync(id);
            if (stage == null)
                return NotFound();

            ViewBag.Engineers = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            return View(stage);
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Name,Sequence,Cost,Status,AssignedEngineerId,DepartmentId,PlannedStartDate,PlannedEndDate,ActualStartDate,ActualEndDate,WorkDocumentation")] ProjectStage model)
        {
            var stage = await _context.ProjectStages.FindAsync(id);
            if (stage == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Engineers = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
                ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
                return View(model);
            }

            // ملاحظة: الوزن (Weight) لا يمكن تعديله بعد الإنشاء حسب قواعد العمل (القسم 6.1)
            stage.Name = model.Name;
            stage.Sequence = model.Sequence;
            stage.Cost = model.Cost;
            stage.Status = model.Status;
            stage.AssignedEngineerId = string.IsNullOrEmpty(model.AssignedEngineerId) ? null : model.AssignedEngineerId;
            stage.DepartmentId = model.DepartmentId;
            stage.PlannedStartDate = model.PlannedStartDate;
            stage.PlannedEndDate = model.PlannedEndDate;
            stage.ActualStartDate = model.ActualStartDate;
            stage.ActualEndDate = model.ActualEndDate;
            stage.WorkDocumentation = model.WorkDocumentation;

            await _context.SaveChangesAsync();
            await _calc.RecalculateProjectAsync(stage.ProjectId);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ProjectStage), stage.Id.ToString(), $"تعديل مرحلة: {stage.Name}");

            TempData["Success"] = $"تم تحديث المرحلة {stage.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
        }

        // ============================================
        // حذف مرحلة (يحذف خطواتها تلقائياً، ويفصل مهامها بدلاً من حذفها لأن علاقة المهمة بالمرحلة Restrict)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var stage = await _context.ProjectStages.FindAsync(id);
            if (stage == null)
                return NotFound();

            var projectId = stage.ProjectId;
            var stageName = stage.Name;

            var linkedTasks = await _context.ProjectTasks.Where(t => t.StageId == id).ToListAsync();
            foreach (var t in linkedTasks)
                t.StageId = null;
            if (linkedTasks.Count > 0)
                await _context.SaveChangesAsync();

            _context.ProjectStages.Remove(stage);
            await _context.SaveChangesAsync();

            await _calc.RecalculateProjectAsync(projectId);

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(ProjectStage), id.ToString(), $"حذف مرحلة: {stageName}");

            TempData["Success"] = "تم حذف المرحلة بنجاح";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        // ============================================
        // إنشاء خطوة داخل مرحلة (مجموع أوزان الخطوات يجب ألا يتجاوز 100%)
        // ============================================
        [RequirePermission("Projects.Edit")]
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

            await _audit.LogAsync(CurrentUserId, "Create", nameof(ProjectStep), model.Id.ToString(), $"إضافة خطوة: {model.Name} (مرحلة {stage.Name})");

            TempData["Success"] = $"تمت إضافة الخطوة {model.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
        }

        // ============================================
        // تعديل خطوة (الوزن غير قابل للتعديل بعد الإنشاء)
        // إكمال الخطوة يسجّل تاريخ الإكمال والمُكمِل تلقائياً، ويحدّث نسبة إنجاز المرحلة والمشروع
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpGet]
        public async Task<IActionResult> EditStep(int id)
        {
            var step = await _context.ProjectSteps.Include(s => s.Stage).FirstOrDefaultAsync(s => s.Id == id);
            if (step == null)
                return NotFound();

            return View(step);
        }

        [RequirePermission("Projects.Edit")]
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

            if (!wasCompleted && model.Status == StepStatus.Completed)
            {
                step.CompletedDate = DateTime.UtcNow;
                step.CompletedById = CurrentUserId;
            }
            else if (model.Status != StepStatus.Completed)
            {
                step.CompletedDate = null;
                step.CompletedById = null;
            }

            await _context.SaveChangesAsync();
            await _calc.RecalculateStageAsync(step.StageId);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ProjectStep), step.Id.ToString(), $"تعديل خطوة: {step.Name}");

            TempData["Success"] = $"تم تحديث الخطوة {step.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = step.Stage.ProjectId });
        }

        // ============================================
        // حذف خطوة
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStep(int id)
        {
            var step = await _context.ProjectSteps.Include(s => s.Stage).FirstOrDefaultAsync(s => s.Id == id);
            if (step == null)
                return NotFound();

            var stageId = step.StageId;
            var projectId = step.Stage.ProjectId;
            var stepName = step.Name;

            _context.ProjectSteps.Remove(step);
            await _context.SaveChangesAsync();

            await _calc.RecalculateStageAsync(stageId);

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(ProjectStep), id.ToString(), $"حذف خطوة: {stepName}");

            TempData["Success"] = "تم حذف الخطوة بنجاح";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }
    }
}