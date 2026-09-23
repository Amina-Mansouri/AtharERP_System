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
        private readonly ProposalReviewPdfService _pdfService;

        public DesignProposalsController(
            AppDbContext context,
            FileUploadService fileUpload,
            PermissionService permissionService,
            NotificationService notify,
            ProposalReviewPdfService pdfService)
        {
            _context = context;
            _fileUpload = fileUpload;
            _permissionService = permissionService;
            _notify = notify;
            _pdfService = pdfService;
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
        [HttpGet]
        public async Task<IActionResult> Review(int id, int? projectId, int? stageId, string? taskFilter)
        {
            var proposal = await _context.DesignProposals
                .Include(p => p.ProjectTask).ThenInclude(t => t.Stage)
                .Include(p => p.Project).ThenInclude(pr => pr.ParentProject)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
                return NotFound();

            ViewBag.Proposal = proposal;
            ViewBag.CurrentUserName = User.FindFirstValue(ClaimTypes.Name) ?? "";
            ViewBag.ProjectId = projectId;
            ViewBag.StageId = stageId;
            ViewBag.TaskFilter = taskFilter;
            return View();
        }

        [RequirePermission("Projects.Tasks.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(
            int id,
            ProposalStatus status,
            string reviewerName,
            string? reviewerPosition,
            IFormFile? reviewerSignature,
            string? supervisorName,
            string? supervisorPosition,
            IFormFile? supervisorSignature,
            string? notes,
            string? discipline,
            int? projectId,
            int? stageId,
            string? taskFilter)
        {
            var proposal = await _context.DesignProposals
                .Include(p => p.ProjectTask).ThenInclude(t => t.Stage)
                .Include(p => p.Project).ThenInclude(pr => pr.ParentProject)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
                return NotFound();

            proposal.Status = status;
            await _context.SaveChangesAsync();

            var review = new ProposalReview
            {
                DesignProposalId = proposal.Id,
                Status = status,
                ReviewerName = reviewerName,
                ReviewerPosition = reviewerPosition,
                SupervisorName = supervisorName,
                SupervisorPosition = supervisorPosition,
                Notes = notes,
                Discipline = discipline,
                ReviewDate = DateTime.UtcNow,
                ReviewedById = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };

            if (reviewerSignature != null && reviewerSignature.Length > 0)
            {
                var sigResult = await _fileUpload.SaveFileAsync(reviewerSignature, $"reviews/proposal-{proposal.Id}");
                if (sigResult.Success)
                    review.ReviewerSignaturePath = sigResult.FilePath;
            }

            if (supervisorSignature != null && supervisorSignature.Length > 0)
            {
                var sigResult = await _fileUpload.SaveFileAsync(supervisorSignature, $"reviews/proposal-{proposal.Id}");
                if (sigResult.Success)
                    review.SupervisorSignaturePath = sigResult.FilePath;
            }

            _context.ProposalReviews.Add(review);
            await _context.SaveChangesAsync();

            var subProject = proposal.Project.Scope == ProjectScope.Sub ? proposal.Project : null;
            var mainProject = subProject != null && proposal.Project.ParentProject != null ? proposal.Project.ParentProject : proposal.Project;

            var pdfBytes = _pdfService.Generate(review, mainProject, subProject, proposal.ProjectTask.Stage?.Name, proposal.Name, proposal.Revision);
            var pdfResult = await _fileUpload.SaveGeneratedFileAsync(pdfBytes, $"reviews/proposal-{proposal.Id}", ".pdf");
            if (pdfResult.Success)
            {
                review.PdfFilePath = pdfResult.FilePath;
                await _context.SaveChangesAsync();
            }

            var message = status == ProposalStatus.Approved || status == ProposalStatus.ApprovedWithModification
                ? $"تم اعتماد مقترحك \"{proposal.Name}\""
                : $"تم رفض مقترحك \"{proposal.Name}\" — تحتاج مراجعة";

            await _notify.NotifyAsync(proposal.PreparedById, message,
                NotificationEventType.TaskStatusChanged, review.PdfFilePath ?? $"/ProjectTasks/Edit/{proposal.ProjectTaskId}",
                entityType: "DesignProposal", entityId: proposal.Id);

            TempData["Success"] = "تم إرسال المراجعة بنجاح";
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