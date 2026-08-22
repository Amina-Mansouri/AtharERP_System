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

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

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
        // تعديل مرحلة (لا يمكن تعديل الوزن بعد الإنشاء)
        // ============================================
        [RequirePermission("Projects.Stages.Manage")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var stage = await _context.ProjectStages.FindAsync(id);
            if (stage == null)
                return NotFound();

            await LoadDropdownsAsync();
            return View(stage);
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Name,Sequence,Status,Cost,AssignedEngineerId,DepartmentId,PlannedStartDate,PlannedEndDate,ActualStartDate,ActualEndDate,WorkDocumentation")] ProjectStage model)
        {
            var stage = await _context.ProjectStages.FindAsync(id);
            if (stage == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(model);
            }

            var wasCompleted = stage.Status == StageStatus.Completed;

            stage.Name = model.Name;
            stage.Sequence = model.Sequence;
            stage.Status = model.Status;
            stage.Cost = model.Cost;
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

        private async Task LoadDropdownsAsync()
        {
            ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.Departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        }
    }
}