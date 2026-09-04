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
    // إدارة تكليفات المشروع + الترحيل التلقائي للمالية عند الاكتمال (القسم 5.7/6.5)
    // عزل مالي: المهندسون المصممون لا يرون هذه الصفحة (القسم 6.6.3)
    // الاسم السابق: ProjectCostsController — أُعيدت التسمية حسب 06-CONFLICTS.md · C7
    public class ProjectAssignmentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PermissionService _permissionService;
        private readonly AuditService _audit;
        private readonly NotificationService _notify;

        public ProjectAssignmentsController(
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

        [RequirePermission("Projects.Assignments.View")]
        public async Task<IActionResult> Index(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                return NotFound();

            var assignments = await _context.ProjectAssignments
                .Include(a => a.Subtasks)
                .Include(a => a.Stage)
                .Include(a => a.LeadEngineer)
                .Include(a => a.AssistantEngineer)
                .Where(a => a.ProjectId == projectId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.Project = project;
            ViewBag.CanEdit = await _permissionService.HasPermissionAsync(User, "Projects.Assignments.Edit");

            return View(assignments);
        }

        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProjectId,StageId,CostType,Description,Area,PricePerMeter,Amount,DiscountOrAdditionPercent,IsUrgent,LeadEngineerId,AssistantEngineerId,ReceivedDate,AgreedDate,ActualDate")] ProjectAssignment model)
        {
            var project = await _context.Projects.FindAsync(model.ProjectId);
            if (project == null)
                return NotFound();

            if (model.Area.HasValue && model.PricePerMeter.HasValue)
                model.Amount = model.Area.Value * model.PricePerMeter.Value;

            model.FinalAmount = model.Amount * (1 + model.DiscountOrAdditionPercent / 100);
            model.Status = AssignmentStatus.Pending;
            model.CreatedAt = DateTime.UtcNow;

            _context.ProjectAssignments.Add(model);
            await _context.SaveChangesAsync();

            // أول تكليف للمشروع: تحويل الحالة تلقائياً لـ"قيد التنفيذ" + ترحيل تلقائي للمواقع إن كان مفعّلاً (بند حالة المشروع)
            if (project.Status == ProjectStatus.New)
            {
                project.Status = ProjectStatus.InProgress;

                if (project.AutoTransferToSite && !await _context.Sites.AnyAsync(s => s.ProjectId == project.Id))
                {
                    _context.Sites.Add(new Site
                    {
                        Name = project.Name,
                        ProjectId = project.Id,
                        Status = SiteStatus.Active,
                        StartDate = DateTime.UtcNow,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
            }

            await _audit.LogAsync(CurrentUserId, "Create", nameof(ProjectAssignment), model.Id.ToString(), $"إضافة تكليف: {model.CostType} - {model.FinalAmount:N2}");

            TempData["Success"] = "تمت إضافة التكليف بنجاح";
            return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
        }

        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("StageId,CostType,Description,Area,PricePerMeter,Amount,DiscountOrAdditionPercent,Status,IsUrgent,LeadEngineerId,AssistantEngineerId,ReceivedDate,AgreedDate,ActualDate")] ProjectAssignment model)
        {
            var assignment = await _context.ProjectAssignments.FindAsync(id);
            if (assignment == null)
                return NotFound();

            var wasCompleted = assignment.Status == AssignmentStatus.Completed;

            assignment.StageId = model.StageId;
            assignment.CostType = model.CostType;
            assignment.Description = model.Description;
            assignment.Area = model.Area;
            assignment.PricePerMeter = model.PricePerMeter;

            assignment.Amount = model.Area.HasValue && model.PricePerMeter.HasValue
                ? model.Area.Value * model.PricePerMeter.Value
                : model.Amount;

            assignment.DiscountOrAdditionPercent = model.DiscountOrAdditionPercent;
            assignment.FinalAmount = assignment.Amount * (1 + assignment.DiscountOrAdditionPercent / 100);
            assignment.Status = model.Status;
            assignment.IsUrgent = model.IsUrgent;
            assignment.LeadEngineerId = model.LeadEngineerId;
            assignment.AssistantEngineerId = model.AssistantEngineerId;
            assignment.ReceivedDate = model.ReceivedDate;
            assignment.AgreedDate = model.AgreedDate;
            assignment.ActualDate = model.ActualDate;

            await _context.SaveChangesAsync();

            // الترحيل التلقائي للمالية عند تغيير الحالة إلى مكتمل (القسم 5.7)
            if (!wasCompleted && assignment.Status == AssignmentStatus.Completed)
            {
                await TransferToFinanceAsync(assignment);
            }

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ProjectAssignment), assignment.Id.ToString(), $"تعديل تكليف: {assignment.CostType} - {assignment.FinalAmount:N2}");

            TempData["Success"] = "تم تحديث التكليف بنجاح";
            return RedirectToAction("Details", "Projects", new { id = assignment.ProjectId });
        }

        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var assignment = await _context.ProjectAssignments.FindAsync(id);
            if (assignment == null)
                return NotFound();

            if (assignment.IsTransferredToFinance)
            {
                TempData["Error"] = "لا يمكن حذف تكليف تم ترحيله للمالية بالفعل";
                return RedirectToAction("Details", "Projects", new { id = assignment.ProjectId });
            }

            var projectId = assignment.ProjectId;
            var costType = assignment.CostType;
            _context.ProjectAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(ProjectAssignment), id.ToString(), $"حذف تكليف: {costType}");

            TempData["Success"] = "تم حذف التكليف بنجاح";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        // ============================================
        // المهام الفرعية داخل التكليف (القسم 3.10)
        // ============================================
        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubtask(int projectAssignmentId, string name)
        {
            var assignment = await _context.ProjectAssignments.FindAsync(projectAssignmentId);
            if (assignment == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(name))
            {
                _context.ProjectAssignmentSubtasks.Add(new ProjectAssignmentSubtask { ProjectAssignmentId = projectAssignmentId, Name = name });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Projects", new { id = assignment.ProjectId });
        }

        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubtask(int id, int projectId)
        {
            var subtask = await _context.ProjectAssignmentSubtasks.FindAsync(id);
            if (subtask != null)
            {
                subtask.IsCompleted = !subtask.IsCompleted;
                subtask.CompletedAt = subtask.IsCompleted ? DateTime.UtcNow : null;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubtask(int id, int projectId)
        {
            var subtask = await _context.ProjectAssignmentSubtasks.FindAsync(id);
            if (subtask != null)
            {
                _context.ProjectAssignmentSubtasks.Remove(subtask);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        // ============================================
        // دوال مساعدة
        // ============================================
        private async Task TransferToFinanceAsync(ProjectAssignment assignment)
        {
            assignment.IsTransferredToFinance = true;
            assignment.TransferredToFinanceAt = DateTime.UtcNow;

            _context.FinancialRecords.Add(new FinancialRecord
            {
                ProjectId = assignment.ProjectId,
                ProjectAssignmentId = assignment.Id,
                CostType = assignment.CostType,
                Area = assignment.Area,
                Value = assignment.FinalAmount,
                IsCleared = false,
                CreatedAt = DateTime.UtcNow
            });

            var project = await _context.Projects.FindAsync(assignment.ProjectId);
            if (project != null)
                project.ActualCost += assignment.FinalAmount;

            await _context.SaveChangesAsync();

            var financeUserIds = await GetUsersWithPermissionAsync("Finance.View");
            if (financeUserIds.Count > 0)
                await _notify.NotifyManyAsync(financeUserIds, $"تم ترحيل تكليف {assignment.CostType} إلى المالية بقيمة {assignment.FinalAmount:N2}");
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