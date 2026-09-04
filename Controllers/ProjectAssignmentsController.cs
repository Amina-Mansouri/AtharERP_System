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
        private readonly ProjectCalculationService _calc;

        public ProjectAssignmentsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            PermissionService permissionService,
            AuditService audit,
            NotificationService notify,
            ProjectCalculationService calc)
        {
            _context = context;
            _userManager = userManager;
            _permissionService = permissionService;
            _audit = audit;
            _notify = notify;
            _calc = calc;
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



        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    [Bind("ProjectId,StageId,CostType,Description,IsUrgent,ReceivedDate,AgreedDate,ActualDate")] ProjectAssignment model,
    List<string>? engineerIds,
    List<int>? taskIds)
        {
            var project = await _context.Projects.FindAsync(model.ProjectId);
            if (project == null)
                return NotFound();

            model.FinalAmount = 0;
            model.Status = AssignmentStatus.Pending;
            model.CreatedAt = DateTime.UtcNow;

            _context.ProjectAssignments.Add(model);
            await _context.SaveChangesAsync();

            if (engineerIds != null)
            {
                foreach (var uid in engineerIds.Where(u => !string.IsNullOrEmpty(u)).Distinct())
                {
                    _context.AssignmentEngineers.Add(new AssignmentEngineer { ProjectAssignmentId = model.Id, UserId = uid });
                }
                await _context.SaveChangesAsync();
            }

            if (taskIds != null && taskIds.Any())
            {
                var tasksToLink = await _context.ProjectTasks
                    .Where(t => taskIds.Contains(t.Id) && t.StageId == model.StageId && t.ProjectAssignmentId == null)
                    .ToListAsync();
                foreach (var t in tasksToLink)
                {
                    t.ProjectAssignmentId = model.Id;
                }
                await _context.SaveChangesAsync();
                await _calc.RecalculateAssignmentValueAsync(model.Id);
            }

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

            await _audit.LogAsync(CurrentUserId, "Create", nameof(ProjectAssignment), model.Id.ToString(), $"إضافة تكليف: {model.CostType}");

            TempData["Success"] = "تمت إضافة التكليف بنجاح — سعّري مهامه من شاشة المهام";
            return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
        }

        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
     int id,
     [Bind("StageId,CostType,Description,Status,IsUrgent,ReceivedDate,AgreedDate,ActualDate")] ProjectAssignment model)
        {
            var assignment = await _context.ProjectAssignments.FindAsync(id);
            if (assignment == null)
                return NotFound();

            var wasCompleted = assignment.Status == AssignmentStatus.Completed;

            assignment.StageId = model.StageId;
            assignment.CostType = model.CostType;
            assignment.Description = model.Description;
            assignment.Status = model.Status;
            assignment.IsUrgent = model.IsUrgent;
            assignment.ReceivedDate = model.ReceivedDate;
            assignment.AgreedDate = model.AgreedDate;
            assignment.ActualDate = model.ActualDate;

            await _context.SaveChangesAsync();

            // الترحيل التلقائي للمالية عند تغيير الحالة إلى مكتمل (القسم 5.7)
            if (!wasCompleted && assignment.Status == AssignmentStatus.Completed)
            {
                await TransferToFinanceAsync(assignment);
            }

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ProjectAssignment), assignment.Id.ToString(), $"تعديل تكليف: {assignment.CostType}");

            TempData["Success"] = "تم تحديث التكليف بنجاح";
            return RedirectToAction("Details", "Projects", new { id = assignment.ProjectId });
        }

        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEngineer(int assignmentId, string userId, int projectId)
        {
            var exists = await _context.AssignmentEngineers.AnyAsync(e => e.ProjectAssignmentId == assignmentId && e.UserId == userId);
            if (!exists)
            {
                _context.AssignmentEngineers.Add(new AssignmentEngineer { ProjectAssignmentId = assignmentId, UserId = userId });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEngineer(int id, int projectId)
        {
            var link = await _context.AssignmentEngineers.FindAsync(id);
            if (link != null)
            {
                _context.AssignmentEngineers.Remove(link);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }


        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hold(int id, int projectId)
        {
            var assignment = await _context.ProjectAssignments.FindAsync(id);
            if (assignment != null && assignment.Status != AssignmentStatus.Completed)
            {
                assignment.Status = AssignmentStatus.Pending;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        [RequirePermission("Projects.Assignments.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, int projectId)
        {
            var assignment = await _context.ProjectAssignments.FindAsync(id);
            if (assignment != null)
            {
                assignment.Status = AssignmentStatus.Cancelled;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", "Projects", new { id = projectId });
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
            var linkedTasksCount = await _context.ProjectTasks.CountAsync(t => t.ProjectAssignmentId == id);

            _context.ProjectAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(ProjectAssignment), id.ToString(), $"حذف تكليف: {costType}");

            TempData["Success"] = linkedTasksCount > 0
                ? $"تم حذف التكليف، وتم حذف {linkedTasksCount} مهمة مرتبطة به تلقائيًا"
                : "تم حذف التكليف بنجاح";
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