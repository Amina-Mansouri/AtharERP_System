using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class ClientRedesignRequestsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly ProjectCalculationService _calc;
        private readonly NotificationService _notify;

        public ClientRedesignRequestsController(AppDbContext context, PermissionService permissionService, ProjectCalculationService calc, NotificationService notify)
        {
            _context = context;
            _permissionService = permissionService;
            _calc = calc;
            _notify = notify;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("Projects.Stages.Manage")]
        public async Task<IActionResult> Index(int stageId)
        {
            var stage = await _context.ProjectStages.Include(s => s.Project).FirstOrDefaultAsync(s => s.Id == stageId);
            if (stage == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, stage.ProjectId))
                return Forbid();

            var requests = await _context.ClientRedesignRequests
                .Where(r => r.ProjectStageId == stageId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            ViewBag.Stage = stage;
            ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
            return View(requests);
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProjectStageId,IncreasePercentage,Notes")] ClientRedesignRequest model,
            string redesignTaskTitle,
            List<string>? engineerIds,
            DateTime? plannedStartDate,
            DateTime? plannedEndDate)
        {
            var stage = await _context.ProjectStages.FindAsync(model.ProjectStageId);
            if (stage == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, stage.ProjectId))
                return Forbid();

            if (string.IsNullOrWhiteSpace(redesignTaskTitle))
            {
                TempData["Error"] = "يجب كتابة عنوان مهمة إعادة التصميم";
                return RedirectToAction("Index", new { stageId = model.ProjectStageId });
            }

            if (engineerIds == null || !engineerIds.Any(u => !string.IsNullOrEmpty(u)))
            {
                TempData["Error"] = "يجب اختيار مهندسة واحدة على الأقل لتكليف إعادة التصميم";
                return RedirectToAction("Index", new { stageId = model.ProjectStageId });
            }

            if (!plannedStartDate.HasValue || !plannedEndDate.HasValue)
            {
                TempData["Error"] = "يجب تحديد تاريخ بداية ونهاية لتكليف إعادة التصميم";
                return RedirectToAction("Index", new { stageId = model.ProjectStageId });
            }

            model.RequestDate = DateTime.UtcNow;
            model.CreatedAt = DateTime.UtcNow;
            _context.ClientRedesignRequests.Add(model);
            await _context.SaveChangesAsync();

            await _calc.SyncStageRedesignPercentageAsync(model.ProjectStageId);

            var assignment = new ProjectAssignment
            {
                ProjectId = stage.ProjectId,
                StageId = stage.Id,
                AssignmentType = $"إعادة تصميم: {redesignTaskTitle}",
                IsRedesign = true,
                Status = AssignmentStatus.InProgress,
                PlannedStartDate = plannedStartDate,
                PlannedEndDate = plannedEndDate,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProjectAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            _context.ProjectTasks.Add(new ProjectTask
            {
                ProjectId = stage.ProjectId,
                StageId = stage.Id,
                ProjectAssignmentId = assignment.Id,
                Title = redesignTaskTitle,
                Weight = 0,
                Status = ProjectTaskStatus.NotStarted,
                Priority = TaskPriority.Medium,
                PlannedStartDate = plannedStartDate,
                PlannedEndDate = plannedEndDate,
                CreatedAt = DateTime.UtcNow,
                CreatedById = CurrentUserId
            });

            foreach (var uid in engineerIds.Where(u => !string.IsNullOrEmpty(u)).Distinct())
            {
                _context.AssignmentEngineers.Add(new AssignmentEngineer { ProjectAssignmentId = assignment.Id, UserId = uid });
                await EnsureTeamMembershipAsync(stage.ProjectId, uid);
            }
            await _context.SaveChangesAsync();

            foreach (var uid in engineerIds.Where(u => !string.IsNullOrEmpty(u)).Distinct())
            {
                await _notify.NotifyAsync(uid, $"تم تكليفك بإعادة تصميم: {redesignTaskTitle}", NotificationEventType.TaskAssigned, "/ProjectAssignments/MyAssignments", entityType: "ProjectAssignment", entityId: assignment.Id);
            }

            await _calc.RecalculateStageAsync(stage.Id);

            TempData["Success"] = "تم تسجيل طلب إعادة التصميم وإنشاء التكليف بنجاح";
            return RedirectToAction("Index", new { stageId = model.ProjectStageId });
        }

        private async Task EnsureTeamMembershipAsync(int projectId, string userId)
        {
            var exists = await _context.ProjectTeamMembers.AnyAsync(tm => tm.ProjectId == projectId && tm.UserId == userId);
            if (!exists)
            {
                _context.ProjectTeamMembers.Add(new ProjectTeamMember
                {
                    ProjectId = projectId,
                    UserId = userId,
                    Role = TeamRole.Engineer,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.ClientRedesignRequests
                .Include(r => r.ProjectStage)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, request.ProjectStage.ProjectId))
                return Forbid();

            var stageId = request.ProjectStageId;
            _context.ClientRedesignRequests.Remove(request);
            await _context.SaveChangesAsync();

            await _calc.SyncStageRedesignPercentageAsync(stageId);

            TempData["Success"] = "تم حذف الطلب بنجاح";
            return RedirectToAction("Index", new { stageId });
        }
    }
}