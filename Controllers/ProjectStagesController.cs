using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    public class ProjectStagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ProjectCalculationService _calc;

        public ProjectStagesController(AppDbContext context, ProjectCalculationService calc)
        {
            _context = context;
            _calc = calc;
        }

        // ============================================
        // إنشاء مرحلة جديدة (نموذج مضمّن داخل صفحة تفاصيل المشروع)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProjectId,Name,Order,Weight,Cost,AssignedEngineerId")] ProjectStage model)
        {
            var project = await _context.Projects.FindAsync(model.ProjectId);
            if (project == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات المرحلة غير صحيحة";
                return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
            }

            model.Status = ProjectStatus.New;
            model.CompletionPercentage = 0;

            _context.ProjectStages.Add(model);
            await _context.SaveChangesAsync();

            await _calc.RecalculateProjectAsync(model.ProjectId);

            TempData["Success"] = $"تمت إضافة المرحلة {model.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
        }

        // ============================================
        // تعديل مرحلة (لا يمكن تعديل الوزن)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var stage = await _context.ProjectStages.FindAsync(id);
            if (stage == null)
                return NotFound();

            ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            return View(stage);
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Name,Order,Cost,Status,AssignedEngineerId")] ProjectStage model)
        {
            var stage = await _context.ProjectStages.FindAsync(id);
            if (stage == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
                return View(model);
            }

            // ملاحظة: الوزن (Weight) لا يمكن تعديله بعد الإنشاء حسب قواعد العمل
            stage.Name = model.Name;
            stage.Order = model.Order;
            stage.Cost = model.Cost;
            stage.Status = model.Status;
            stage.AssignedEngineerId = model.AssignedEngineerId;

            await _context.SaveChangesAsync();
            await _calc.RecalculateProjectAsync(stage.ProjectId);

            TempData["Success"] = $"تم تحديث المرحلة {stage.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
        }

        // ============================================
        // حذف مرحلة (يحذف خطواتها ومهامها تلقائياً)
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

            _context.ProjectStages.Remove(stage);
            await _context.SaveChangesAsync();

            await _calc.RecalculateProjectAsync(projectId);

            TempData["Success"] = "تم حذف المرحلة بنجاح";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        // ============================================
        // إنشاء خطوة داخل مرحلة
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStep(
            [Bind("ProjectStageId,Name,Weight,ActualCost")] ProjectStep model)
        {
            var stage = await _context.ProjectStages.Include(s => s.Steps).FirstOrDefaultAsync(s => s.Id == model.ProjectStageId);
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

            model.Status = ProjectStatus.New;
            _context.ProjectSteps.Add(model);
            await _context.SaveChangesAsync();

            await _calc.RecalculateStageAsync(stage.Id);

            TempData["Success"] = $"تمت إضافة الخطوة {model.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = stage.ProjectId });
        }

        // ============================================
        // تعديل خطوة (الوزن غير قابل للتعديل بعد الإنشاء)
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpGet]
        public async Task<IActionResult> EditStep(int id)
        {
            var step = await _context.ProjectSteps.Include(s => s.ProjectStage).FirstOrDefaultAsync(s => s.Id == id);
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
            var step = await _context.ProjectSteps.Include(s => s.ProjectStage).FirstOrDefaultAsync(s => s.Id == id);
            if (step == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            step.Name = model.Name;
            step.Status = model.Status;
            step.ActualCost = model.ActualCost;

            await _context.SaveChangesAsync();
            await _calc.RecalculateStageAsync(step.ProjectStageId);

            TempData["Success"] = $"تم تحديث الخطوة {step.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = step.ProjectStage.ProjectId });
        }

        // ============================================
        // حذف خطوة
        // ============================================
        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStep(int id)
        {
            var step = await _context.ProjectSteps.Include(s => s.ProjectStage).FirstOrDefaultAsync(s => s.Id == id);
            if (step == null)
                return NotFound();

            var stageId = step.ProjectStageId;
            var projectId = step.ProjectStage.ProjectId;

            _context.ProjectSteps.Remove(step);
            await _context.SaveChangesAsync();

            await _calc.RecalculateStageAsync(stageId);

            TempData["Success"] = "تم حذف الخطوة بنجاح";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }
    }
}