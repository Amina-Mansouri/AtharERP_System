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
    // إدارة تكاليف المشروع + الترحيل التلقائي للمالية عند الاكتمال (القسم 5.7/6.5)
    // عزل مالي: المهندسون المصممون لا يرون هذه الصفحة (القسم 6.6.3)
    public class ProjectCostsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PermissionService _permissionService;
        private readonly AuditService _audit;
        private readonly NotificationService _notify;

        public ProjectCostsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            PermissionService permissionService,
            AuditService audit,
            NotificationService notify)
        {
            _context = context;
            _userManager = userManager;
            _permissionService = permissionService;
            _audit = audit;
            _notify = notify;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("Projects.Costs.View")]
        public async Task<IActionResult> Index(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                return NotFound();

            var costs = await _context.ProjectCosts
                .Include(c => c.Subtasks)
                .Where(c => c.ProjectId == projectId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Project = project;
            ViewBag.CanEdit = await _permissionService.HasPermissionAsync(User, "Projects.Costs.Edit");

            return View(costs);
        }

        [RequirePermission("Projects.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProjectId,CostType,Description,Area,PricePerMeter,Amount,DiscountOrAdditionPercent")] ProjectCost model)
        {
            var project = await _context.Projects.FindAsync(model.ProjectId);
            if (project == null)
                return NotFound();

            if (model.Area.HasValue && model.PricePerMeter.HasValue)
                model.Amount = model.Area.Value * model.PricePerMeter.Value;

            model.FinalAmount = model.Amount * (1 + model.DiscountOrAdditionPercent / 100);
            model.Status = CostStatus.Pending;
            model.CreatedAt = DateTime.UtcNow;

            _context.ProjectCosts.Add(model);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Create", nameof(ProjectCost), model.Id.ToString(), $"إضافة بند تكلفة: {model.CostType} - {model.FinalAmount:N2}");

            TempData["Success"] = "تمت إضافة بند التكلفة بنجاح";
            return RedirectToAction("Index", new { projectId = model.ProjectId });
        }

        [RequirePermission("Projects.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("CostType,Description,Area,PricePerMeter,Amount,DiscountOrAdditionPercent,Status")] ProjectCost model)
        {
            var cost = await _context.ProjectCosts.FindAsync(id);
            if (cost == null)
                return NotFound();

            var wasCompleted = cost.Status == CostStatus.Completed;

            cost.CostType = model.CostType;
            cost.Description = model.Description;
            cost.Area = model.Area;
            cost.PricePerMeter = model.PricePerMeter;

            cost.Amount = model.Area.HasValue && model.PricePerMeter.HasValue
                ? model.Area.Value * model.PricePerMeter.Value
                : model.Amount;

            cost.DiscountOrAdditionPercent = model.DiscountOrAdditionPercent;
            cost.FinalAmount = cost.Amount * (1 + cost.DiscountOrAdditionPercent / 100);
            cost.Status = model.Status;

            await _context.SaveChangesAsync();

            // الترحيل التلقائي للمالية عند تغيير الحالة إلى مكتمل (القسم 5.7)
            if (!wasCompleted && cost.Status == CostStatus.Completed)
            {
                await TransferToFinanceAsync(cost);
            }

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ProjectCost), cost.Id.ToString(), $"تعديل بند تكلفة: {cost.CostType} - {cost.FinalAmount:N2}");

            TempData["Success"] = "تم تحديث بند التكلفة بنجاح";
            return RedirectToAction("Index", new { projectId = cost.ProjectId });
        }

        [RequirePermission("Projects.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cost = await _context.ProjectCosts.FindAsync(id);
            if (cost == null)
                return NotFound();

            if (cost.IsTransferredToFinance)
            {
                TempData["Error"] = "لا يمكن حذف بند تكلفة تم ترحيله للمالية بالفعل";
                return RedirectToAction("Index", new { projectId = cost.ProjectId });
            }

            var projectId = cost.ProjectId;
            var costType = cost.CostType;
            _context.ProjectCosts.Remove(cost);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(ProjectCost), id.ToString(), $"حذف بند تكلفة: {costType}");

            TempData["Success"] = "تم حذف بند التكلفة بنجاح";
            return RedirectToAction("Index", new { projectId });
        }

        // ============================================
        // المهام الفرعية داخل بند التكلفة (القسم 3.10)
        // ============================================
        [RequirePermission("Projects.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubtask(int projectCostId, string name)
        {
            var cost = await _context.ProjectCosts.FindAsync(projectCostId);
            if (cost == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(name))
            {
                _context.ProjectCostSubtasks.Add(new ProjectCostSubtask { ProjectCostId = projectCostId, Name = name });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", new { projectId = cost.ProjectId });
        }

        [RequirePermission("Projects.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubtask(int id, int projectId)
        {
            var subtask = await _context.ProjectCostSubtasks.FindAsync(id);
            if (subtask != null)
            {
                subtask.IsCompleted = !subtask.IsCompleted;
                subtask.CompletedAt = subtask.IsCompleted ? DateTime.UtcNow : null;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", new { projectId });
        }

        [RequirePermission("Projects.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubtask(int id, int projectId)
        {
            var subtask = await _context.ProjectCostSubtasks.FindAsync(id);
            if (subtask != null)
            {
                _context.ProjectCostSubtasks.Remove(subtask);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", new { projectId });
        }

        // ============================================
        // دوال مساعدة
        // ============================================
        private async Task TransferToFinanceAsync(ProjectCost cost)
        {
            cost.IsTransferredToFinance = true;
            cost.TransferredToFinanceAt = DateTime.UtcNow;

            _context.FinancialRecords.Add(new FinancialRecord
            {
                ProjectId = cost.ProjectId,
                ProjectCostId = cost.Id,
                CostType = cost.CostType,
                Area = cost.Area,
                Value = cost.FinalAmount,
                IsCleared = false,
                CreatedAt = DateTime.UtcNow
            });

            var project = await _context.Projects.FindAsync(cost.ProjectId);
            if (project != null)
                project.ActualCost += cost.FinalAmount;

            await _context.SaveChangesAsync();

            var financeUserIds = await GetUsersWithPermissionAsync("Finance.View");
            if (financeUserIds.Count > 0)
                await _notify.NotifyManyAsync(financeUserIds, $"تم ترحيل تكلفة {cost.CostType} إلى المالية بقيمة {cost.FinalAmount:N2}");
        }

        private async Task<List<string>> GetUsersWithPermissionAsync(string permissionCode)
        {
            var roleIds = await _context.RolePermissions
                .Where(rp => rp.IsGranted && rp.Permission.Code == permissionCode)
                .Select(rp => rp.RoleId)
                .ToListAsync();

            var roleNames = await _context.Roles
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Name!)
                .ToListAsync();

            var userIds = new HashSet<string>();
            foreach (var roleName in roleNames)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                foreach (var u in usersInRole)
                    userIds.Add(u.Id);
            }

            return userIds.ToList();
        }
    }
}