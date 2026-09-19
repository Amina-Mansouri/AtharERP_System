using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class DesignProposalsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FileUploadService _fileUpload;
        private readonly PermissionService _permissionService;
        private readonly NotificationService _notify;

        public DesignProposalsController(
            AppDbContext context,
            FileUploadService fileUpload,
            PermissionService permissionService,
            NotificationService notify)
        {
            _context = context;
            _fileUpload = fileUpload;
            _permissionService = permissionService;
            _notify = notify;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task<bool> CanExecuteTaskAsync(ProjectTask task)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int taskId, string code, string name, int revision, IFormFile file)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            if (!await CanExecuteTaskAsync(task))
                return Forbid();

            if (await IsAssignmentLockedAsync(task))
            {
                TempData["Error"] = "التكليف معلَّق أو ملغى — لا يمكن رفع مقترحات له حالياً";
                return RedirectToAction("Edit", "ProjectTasks", new { id = taskId });
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "الرجاء اختيار ملف المقترح";
                return RedirectToAction("Edit", "ProjectTasks", new { id = taskId });
            }

            var result = await _fileUpload.SaveFileAsync(file, $"proposals/{task.ProjectId}");
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Edit", "ProjectTasks", new { id = taskId });
            }

            var proposal = new DesignProposal
            {
                ProjectId = task.ProjectId,
                ProjectTaskId = task.Id,
                Code = code,
                Name = name,
                Revision = revision < 1 ? 1 : revision,
                PreparedById = CurrentUserId,
                SubmittedDate = DateTime.UtcNow,
                FileName = file.FileName,
                FilePath = result.FilePath!,
                FileType = result.FileType,
                FileSize = result.FileSize,
                Status = ProposalStatus.Submitted
            };
            _context.DesignProposals.Add(proposal);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم رفع المقترح، بانتظار اعتماد المدير";
            return RedirectToAction("Edit", "ProjectTasks", new { id = taskId });
        }

        [RequirePermission("Projects.Tasks.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? comment, int? projectId, int? stageId, string? taskFilter)
        {
            var proposal = await _context.DesignProposals.FirstOrDefaultAsync(p => p.Id == id);
            if (proposal == null)
                return NotFound();

            proposal.Status = ProposalStatus.Approved;
            proposal.ManagerComment = comment;
            await _context.SaveChangesAsync();

            await _notify.NotifyAsync(proposal.PreparedById,
                string.IsNullOrWhiteSpace(comment) ? $"تم اعتماد مقترحك \"{proposal.Name}\"" : $"تم اعتماد مقترحك \"{proposal.Name}\" — {comment}",
                NotificationEventType.TaskStatusChanged, $"/ProjectTasks/Edit/{proposal.ProjectTaskId}", entityType: "DesignProposal", entityId: proposal.Id);

            TempData["Success"] = "تم اعتماد المقترح";
            return RedirectToAction("Overview", "ProjectAssignments", new { projectId, stageId, taskFilter });
        }

        [RequirePermission("Projects.Tasks.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? comment, int? projectId, int? stageId, string? taskFilter)
        {
            var proposal = await _context.DesignProposals.FirstOrDefaultAsync(p => p.Id == id);
            if (proposal == null)
                return NotFound();

            proposal.Status = ProposalStatus.Rejected;
            proposal.ManagerComment = comment;
            await _context.SaveChangesAsync();

            await _notify.NotifyAsync(proposal.PreparedById,
                string.IsNullOrWhiteSpace(comment) ? $"تم رفض مقترحك \"{proposal.Name}\"" : $"تم رفض مقترحك \"{proposal.Name}\" — {comment}",
                NotificationEventType.TaskStatusChanged, $"/ProjectTasks/Edit/{proposal.ProjectTaskId}", entityType: "DesignProposal", entityId: proposal.Id);

            TempData["Success"] = "تم رفض المقترح";
            return RedirectToAction("Overview", "ProjectAssignments", new { projectId, stageId, taskFilter });
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
    }
}