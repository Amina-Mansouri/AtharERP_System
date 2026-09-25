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
        private readonly ProjectCalculationService _calc;

        public DesignProposalsController(
            AppDbContext context,
            FileUploadService fileUpload,
            PermissionService permissionService,
            NotificationService notify,
            ProposalReviewPdfService pdfService,
            ProjectCalculationService calc)
        {
            _context = context;
            _fileUpload = fileUpload;
            _permissionService = permissionService;
            _notify = notify;
            _pdfService = pdfService;
            _calc = calc;
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

        private async Task<List<string>> GetTaskWorkerIdsAsync(ProjectTask task)
        {
            if (!task.ProjectAssignmentId.HasValue)
                return new List<string>();

            return await _context.AssignmentEngineers
                .Where(e => e.ProjectAssignmentId == task.ProjectAssignmentId.Value)
                .Select(e => e.UserId)
                .Distinct()
                .ToListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int taskId, IFormFile file, DocumentClassification classification, FileCategory fileCategory, int? assignmentId)
        {
            var task = await _context.ProjectTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            if (!await CanExecuteTaskAsync(task))
                return Forbid();

            IActionResult BackToTask() => assignmentId.HasValue
                ? this.RedirectKeepingTab("ManageTasks", "ProjectAssignments", new { id = assignmentId.Value })
                : this.RedirectKeepingTab("Edit", "ProjectTasks", new { id = taskId });

            if (await IsAssignmentLockedAsync(task))
            {
                TempData["Error"] = "التكليف معلَّق أو ملغى — لا يمكن رفع مستندات له حالياً";
                return BackToTask();
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "الرجاء اختيار ملف";
                return BackToTask();
            }

            var result = await _fileUpload.SaveFileAsync(file, $"proposals/{task.ProjectId}");
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return BackToTask();
            }

            // رقم المستند: تسلسلي عام لكل مستندات المشروع
            var globalDocNumber = await _context.DesignProposals.Where(d => d.ProjectId == task.ProjectId).CountAsync() + 1;

            // رقم تسلسلي داخل التكليف
            var seqInAssignment = task.ProjectAssignmentId.HasValue
                ? await _context.DesignProposals.Where(d => d.ProjectTask.ProjectAssignmentId == task.ProjectAssignmentId.Value).CountAsync() + 1
                : 1;

            // رقم الإصدار: عدد مرات إرسال ملف لنفس البند
            var version = await _context.DesignProposals.Where(d => d.ProjectTaskId == taskId).CountAsync() + 1;

            var code = $"{task.Project.Code}-{globalDocNumber:D3}-{classification}-{fileCategory}-{seqInAssignment:D2}-{version:D2}";

            var proposal = new DesignProposal
            {
                ProjectId = task.ProjectId,
                ProjectTaskId = task.Id,
                Code = code,
                Name = task.Title,
                Revision = version,
                Classification = classification,
                FileCategory = fileCategory,
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

            if (task.Status == ProjectTaskStatus.Completed)
            {
                task.Status = ProjectTaskStatus.PendingReview;
                await _context.SaveChangesAsync();
                if (task.StageId.HasValue)
                    await _calc.RecalculateStageAsync(task.StageId.Value);
            }

            TempData["Success"] = "تم رفع المستند، بانتظار الاعتماد";
            return BackToTask();
        }
        [RequirePermission("Projects.Tasks.Manage")]
        [HttpGet]
        public async Task<IActionResult> Review(int id, int? projectId, int? stageId, string? taskFilter)
        {
            var proposal = await _context.DesignProposals
                .Include(p => p.ProjectTask).ThenInclude(t => t.Stage)
                .Include(p => p.Project).ThenInclude(pr => pr.ParentProject)
                .Include(p => p.Project).ThenInclude(pr => pr.ProjectCategory)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
                return NotFound();

            var reviewer = await _context.Users.Include(u => u.JobRankRef).FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            ApplicationUser? supervisor = null;
            var supervisorId = proposal.ProjectTask.Stage?.AssignedEngineerId;
            if (!string.IsNullOrEmpty(supervisorId))
                supervisor = await _context.Users.Include(u => u.JobRankRef).FirstOrDefaultAsync(u => u.Id == supervisorId);

            var assignmentId = proposal.ProjectTask.ProjectAssignmentId;
            int lastReviewNumber = 0;
            if (assignmentId.HasValue)
            {
                lastReviewNumber = await _context.ProposalReviews
                    .Where(r => r.DesignProposal != null && r.DesignProposal.ProjectTask.ProjectAssignmentId == assignmentId.Value)
                    .Select(r => (int?)r.ReviewNumber)
                    .MaxAsync() ?? 0;
            }

            ViewBag.Proposal = proposal;
            ViewBag.ReviewerName = reviewer?.FullName;
            ViewBag.ReviewerPosition = reviewer?.JobRankRef?.NameAr;
            ViewBag.ReviewerSignaturePath = reviewer?.SignatureImagePath;
            ViewBag.SupervisorName = supervisor?.FullName;
            ViewBag.SupervisorPosition = supervisor?.JobRankRef?.NameAr;
            ViewBag.SupervisorSignaturePath = supervisor?.SignatureImagePath;
            ViewBag.SuggestedReviewNumber = lastReviewNumber + 1;
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
            string? notes,
            string? discipline,
            int reviewNumber,
            int? projectId,
            int? stageId,
            string? taskFilter)
        {
            var proposal = await _context.DesignProposals
                .Include(p => p.ProjectTask).ThenInclude(t => t.Stage)
                .Include(p => p.Project).ThenInclude(pr => pr.ParentProject)
                .Include(p => p.Project).ThenInclude(pr => pr.ProjectCategory)
                .Include(p => p.PreparedBy)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
                return NotFound();

            var task = proposal.ProjectTask;

            var reviewer = await _context.Users.Include(u => u.JobRankRef).FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            ApplicationUser? supervisor = null;
            var supervisorId = task.Stage?.AssignedEngineerId;
            if (!string.IsNullOrEmpty(supervisorId))
                supervisor = await _context.Users.Include(u => u.JobRankRef).FirstOrDefaultAsync(u => u.Id == supervisorId);

            proposal.Status = status;

            var review = new ProposalReview
            {
                DesignProposalId = proposal.Id,
                Status = status,
                ReviewerName = reviewer?.FullName ?? "",
                ReviewerPosition = reviewer?.JobRankRef?.NameAr,
                ReviewerSignaturePath = reviewer?.SignatureImagePath,
                SupervisorName = supervisor?.FullName,
                SupervisorPosition = supervisor?.JobRankRef?.NameAr,
                SupervisorSignaturePath = supervisor?.SignatureImagePath,
                Notes = notes,
                Discipline = discipline,
                ReviewNumber = reviewNumber,
                ReviewDate = DateTime.UtcNow,
                ReviewedById = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProposalReviews.Add(review);
            await _context.SaveChangesAsync();

            var allDocs = await _context.DesignProposals.Where(d => d.ProjectTaskId == task.Id).ToListAsync();
            bool allApproved = allDocs.Any() && allDocs.All(d => d.Status == ProposalStatus.Approved);

            if (task.Status != ProjectTaskStatus.Blocked)
            {
                var previousStatus = task.Status;
                task.Status = allApproved ? ProjectTaskStatus.Completed : ProjectTaskStatus.PendingReview;

                if (status == ProposalStatus.Resubmission || status == ProposalStatus.Redesign)
                    task.RejectionCount++;

                await _context.SaveChangesAsync();

                if (task.StageId.HasValue)
                    await _calc.RecalculateStageAsync(task.StageId.Value);

                if (previousStatus != task.Status)
                {
                    var workerIds = await GetTaskWorkerIdsAsync(task);
                    var message = allApproved
                        ? $"تم اعتماد جميع مستندات مهمتك \"{task.Title}\""
                        : (status == ProposalStatus.Resubmission || status == ProposalStatus.Redesign
                            ? $"تحتاج مستنداً معدَّلاً لمهمتك \"{task.Title}\""
                            : $"تم اعتماد مستند من مهمتك \"{task.Title}\" — بانتظار بقية المستندات");

                    foreach (var workerId in workerIds)
                    {
                        await _notify.NotifyAsync(workerId, message, NotificationEventType.TaskStatusChanged, $"/ProjectTasks/Edit/{task.Id}",
                            requiresAction: status == ProposalStatus.Resubmission || status == ProposalStatus.Redesign,
                            entityType: "ProjectTask", entityId: task.Id);
                    }
                }
            }

            if (task.ProjectAssignmentId.HasValue)
            {
                var assignment = await _context.ProjectAssignments.FirstOrDefaultAsync(a => a.Id == task.ProjectAssignmentId.Value);
                if (assignment != null && assignment.Status != AssignmentStatus.Cancelled)
                {
                    var assignmentTasks = await _context.ProjectTasks.Where(t => t.ProjectAssignmentId == assignment.Id).ToListAsync();
                    bool allTasksCompleted = assignmentTasks.Any() && assignmentTasks.All(t => t.Status == ProjectTaskStatus.Completed);
                    assignment.Status = allTasksCompleted ? AssignmentStatus.Completed : AssignmentStatus.InProgress;
                    await _context.SaveChangesAsync();
                }
            }

            var subProject = proposal.Project.Scope == ProjectScope.Sub ? proposal.Project : null;
            var mainProject = subProject != null && proposal.Project.ParentProject != null ? proposal.Project.ParentProject : proposal.Project;

            var pdfBytes = _pdfService.Generate(review, mainProject, subProject, task.Stage?.Name, proposal.Name, proposal.Revision);
            var pdfResult = await _fileUpload.SaveGeneratedFileAsync(pdfBytes, $"reviews/proposal-{proposal.Id}", ".pdf");
            if (pdfResult.Success)
            {
                review.PdfFilePath = pdfResult.FilePath;
                await _context.SaveChangesAsync();
            }

            var notifyMessage = status == ProposalStatus.Approved || status == ProposalStatus.ApprovedWithModification
                ? $"تم اعتماد مستندك \"{proposal.Name}\""
                : $"تم رفض مستندك \"{proposal.Name}\" — تحتاج مراجعة";

            await _notify.NotifyAsync(proposal.PreparedById, notifyMessage,
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