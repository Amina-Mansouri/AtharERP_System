using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace AtharERP_System.Controllers
{
    [Authorize]
    public class DesignProposalsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FileUploadService _fileUpload;
        private readonly PermissionService _permissionService;
        private readonly NotificationService _notify;
        private readonly ProposalReviewPdfService _pdfService;
        private readonly ProjectCalculationService _calc;

        private readonly IWebHostEnvironment _environment;

        public DesignProposalsController(
            AppDbContext context,
            FileUploadService fileUpload,
            PermissionService permissionService,
            NotificationService notify,
            ProposalReviewPdfService pdfService,
            ProjectCalculationService calc,
            IWebHostEnvironment environment)
        {
            _context = context;
            _fileUpload = fileUpload;
            _permissionService = permissionService;
            _notify = notify;
            _pdfService = pdfService;
            _calc = calc;
            _environment = environment;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task<bool> CanExecuteTaskAsync(ProjectTask task)
        {
            if (await _permissionService.HasPermissionAsync(User, "Projects.Tasks.Manage"))
                return await _permissionService.CanAccessProjectAsync(User, task.ProjectId);

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
        private async Task<List<string>> GetProjectManagersAsync(int projectId)
        {
            var recipientIds = await _permissionService.GetProjectRecipientsAsync(projectId);
            if (!recipientIds.Any())
                return new List<string>();

            return await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                join rp in _context.RolePermissions on r.Id equals rp.RoleId
                join p in _context.Permissions on rp.PermissionId equals p.Id
                where recipientIds.Contains(ur.UserId)
                    && r.IsActive && rp.IsGranted && p.IsActive
                    && p.Code == "Projects.Tasks.Manage"
                select ur.UserId
            ).Distinct().ToListAsync();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int todoId, IFormFile file, FileCategory fileCategory, int? assignmentId)
        {
            var todo = await _context.TaskTodos
                .Include(t => t.Task).ThenInclude(task => task.Project)
                .Include(t => t.Task).ThenInclude(task => task.Stage!).ThenInclude(s => s.DisciplineDepartment)
                .FirstOrDefaultAsync(t => t.Id == todoId);
            if (todo == null)
                return NotFound();

            var task = todo.Task;

            if (!await CanExecuteTaskAsync(task))
                return Forbid();

            IActionResult BackToTask() => assignmentId.HasValue
     ? this.RedirectKeepingTab("ManageTasks", "ProjectAssignments", new { id = assignmentId.Value, taskId = task.Id })
     : this.RedirectKeepingTab("Edit", "ProjectTasks", new { id = task.Id });

            if (await IsAssignmentLockedAsync(task))
            {
                TempData["Error"] = "التكليف معلَّق أو ملغى — لا يمكن رفع مستندات له حالياً";
                return BackToTask();
            }

            if (todo.IsCompleted)
            {
                TempData["Error"] = "هذا البند مكتمل بالفعل — لا يمكن رفع مستند جديد له";
                return BackToTask();
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "الرجاء اختيار ملف";
                return BackToTask();
            }
            var classification = task.Stage?.DisciplineDepartment?.Code;
            if (string.IsNullOrWhiteSpace(classification))
            {
                TempData["Error"] = "لم يُحدَّد قسم/تخصص لمرحلة هذا التكليف بعد — يُرجى تحديد ذلك من بيانات المرحلة أولاً قبل رفع مستند";
                return BackToTask();
            }
            var result = await _fileUpload.SaveFileAsync(file, $"proposals/{task.ProjectId}");
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return BackToTask();
            }

            var seqInAssignment = task.ProjectAssignmentId.HasValue
     ? await _context.DesignProposals.Where(d => d.TaskTodo.Task.ProjectAssignmentId == task.ProjectAssignmentId.Value).CountAsync() + 1
     : 1;

            var version = await _context.DesignProposals.Where(d => d.TaskTodoId == todoId).CountAsync() + 1;

            var code = $"{task.Project.Code}-{fileCategory}-{classification}-{seqInAssignment:D2}-{version:D2}";

            var proposal = new DesignProposal
            {
                ProjectId = task.ProjectId,
                TaskTodoId = todo.Id,
                Code = code,
                Name = todo.Item,
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

            await RecomputeCascadeAsync(todo);

            TempData["Success"] = "تم رفع المستند، بانتظار الاعتماد";
            return BackToTask();
        }

        [HttpGet]
        public async Task<IActionResult> Review(
     int id,
     ProposalStatus status,
     string? notes,
     int? projectId,
          int? stageId,
     string? taskFilter)
        {
            var proposal = await _context.DesignProposals
                .Include(p => p.TaskTodo).ThenInclude(td => td.Task).ThenInclude(t => t!.Stage)
                .Include(p => p.TaskTodo).ThenInclude(td => td.Task).ThenInclude(t => t!.ProjectAssignment)
                .Include(p => p.Project).ThenInclude(pr => pr.ParentProject)
                .Include(p => p.Project).ThenInclude(pr => pr.ProjectCategory)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
                return NotFound();


            if (proposal.Status != ProposalStatus.Submitted)
            {
                TempData["Error"] = "تم اتخاذ قرار نهائي على هذا المستند مسبقاً";
                return RedirectToAction("Overview", "ProjectAssignments", new { projectId, stageId, taskFilter });
            }

            var supervisorId = proposal.TaskTodo.Task.Stage?.AssignedEngineerId;
            var existingReview = await _context.ProposalReviews.FirstOrDefaultAsync(r => r.DesignProposalId == proposal.Id);
            var isSupervisorPhase = existingReview == null && !string.IsNullOrEmpty(supervisorId);

            if (isSupervisorPhase)
            {
                if (CurrentUserId != supervisorId)
                    return Forbid();
            }
            else if (!await _permissionService.HasPermissionAsync(User, "Projects.Tasks.Manage") || !await _permissionService.CanAccessProjectAsync(User, proposal.ProjectId))
            {
                return Forbid();
            }

            var reviewer = await _context.Users.Include(u => u.JobRankRef).FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            ApplicationUser? supervisor = null;
            if (!string.IsNullOrEmpty(supervisorId))
                supervisor = await _context.Users.Include(u => u.JobRankRef).FirstOrDefaultAsync(u => u.Id == supervisorId);

            var lastReviewNumber = await _context.ProposalReviews
     .Where(r => r.DesignProposal != null && r.DesignProposal.TaskTodoId == proposal.TaskTodoId)
     .Select(r => (int?)r.ReviewNumber)
     .MaxAsync() ?? 0;

            // "رقم/اسم المبنى" = المشروع الفرعي؛ إذا كان مشروع المقترح نفسه فرعياً، فالمشروع الرئيسي هو والده (مطابق لمنطق توليد الـ PDF في Review POST)
            var subProject = proposal.Project.Scope == ProjectScope.Sub ? proposal.Project : null;
            var mainProject = subProject != null && proposal.Project.ParentProject != null
                ? proposal.Project.ParentProject
                : proposal.Project;

            ViewBag.Proposal = proposal;
            ViewBag.ReviewPhase = isSupervisorPhase ? "Supervisor" : "Manager";
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
            ViewBag.MainProject = mainProject;
            ViewBag.SubProject = subProject;
            ViewBag.ProjectCategoryLabel = mainProject.ProjectCategory?.DisplayName ?? "-";
            ViewBag.ReviewDate = DateTime.UtcNow;
            ViewBag.AssignmentType = proposal.TaskTodo?.Task?.ProjectAssignment?.AssignmentType;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(
int id,
ProposalStatus status,
string? notes,
int? projectId,
int? stageId,
string? taskFilter,
IFormFile? attachment)
        {
            var proposal = await _context.DesignProposals
                .Include(p => p.TaskTodo).ThenInclude(td => td.Task).ThenInclude(t => t!.Stage)
                .Include(p => p.TaskTodo).ThenInclude(td => td.Task).ThenInclude(t => t!.ProjectAssignment)
                .Include(p => p.Project).ThenInclude(pr => pr.ParentProject)
                .Include(p => p.Project).ThenInclude(pr => pr.ProjectCategory)
                .Include(p => p.PreparedBy)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
                return NotFound();


            if (proposal.Status != ProposalStatus.Submitted)
            {
                TempData["Error"] = "تم اتخاذ قرار نهائي على هذا المستند مسبقاً";
                return RedirectToAction("Overview", "ProjectAssignments", new { projectId, stageId, taskFilter });
            }

            var todo = proposal.TaskTodo;
            var task = todo.Task;
            var supervisorId = task.Stage?.AssignedEngineerId;
            var hasSupervisor = !string.IsNullOrEmpty(supervisorId);

            var existingReview = await _context.ProposalReviews.FirstOrDefaultAsync(r => r.DesignProposalId == proposal.Id);
            var isSupervisorPhase = existingReview == null && hasSupervisor;

            if (isSupervisorPhase)
            {
                if (CurrentUserId != supervisorId)
                    return Forbid();
            }

            else if (!await _permissionService.HasPermissionAsync(User, "Projects.Tasks.Manage") || !await _permissionService.CanAccessProjectAsync(User, proposal.ProjectId))
            {
                return Forbid();
            }

            var actingUser = await _context.Users.Include(u => u.JobRankRef).FirstOrDefaultAsync(u => u.Id == CurrentUserId);
            var isApprovalDecision = status == ProposalStatus.Approved || status == ProposalStatus.ApprovedWithModification;

            var subProject = proposal.Project.Scope == ProjectScope.Sub ? proposal.Project : null;
            var mainProject = subProject != null && proposal.Project.ParentProject != null ? proposal.Project.ParentProject : proposal.Project;

            string? attachmentPath = null;
            string? attachmentFileName = null;
            if (attachment != null && attachment.Length > 0)
            {
                var attachmentResult = await _fileUpload.SaveFileUnrestrictedAsync(attachment, $"review-attachments/proposal-{proposal.Id}");
                if (attachmentResult.Success)
                {
                    attachmentPath = attachmentResult.FilePath;
                    attachmentFileName = attachment.FileName;
                }
            }
          
            // الخطوة الأولى (توصية المشرف بالاعتماد): تُحفَظ وتنتقل للمدير للاعتماد النهائي، دون أي تأثير على حالة المستند/المهمة بعد
            if (isSupervisorPhase && isApprovalDecision)
            {
                var pendingReview = new ProposalReview
                {
                    DesignProposalId = proposal.Id,
                    Status = ProposalStatus.Submitted,
                    ReviewerName = actingUser?.FullName ?? "",
                    ReviewerPosition = actingUser?.JobRankRef?.NameAr,
                    ReviewerSignaturePath = actingUser?.SignatureImagePath,
                    SupervisorName = actingUser?.FullName,
                    SupervisorPosition = actingUser?.JobRankRef?.NameAr,
                    SupervisorSignaturePath = actingUser?.SignatureImagePath,
                    SupervisorStatus = status,
                    SupervisorReviewedAt = DateTime.UtcNow,
                    Notes = notes,
                    AttachmentPath = attachmentPath,
                    AttachmentFileName = attachmentFileName,
                    Discipline = task.ProjectAssignment?.AssignmentType,
                    ReviewNumber = await ComputeNextReviewNumberAsync(proposal.TaskTodoId),
                    ReviewDate = DateTime.UtcNow,
                    ReviewedById = CurrentUserId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProposalReviews.Add(pendingReview);
                await _context.SaveChangesAsync();

                var pendingPdfBytes = _pdfService.Generate(pendingReview, mainProject, subProject, task.Stage?.Name, proposal.Name, proposal.Code);
                var pendingPdfResult = await _fileUpload.SaveGeneratedFileAsync(pendingPdfBytes, $"reviews/proposal-{proposal.Id}", ".pdf");
                if (pendingPdfResult.Success)
                {
                    pendingReview.PdfFilePath = pendingPdfResult.FilePath;
                    await _context.SaveChangesAsync();
                }

                await _notify.NotifyAsync(proposal.PreparedById,
                    $"وافق المهندس المشرف على مستندك \"{proposal.Name}\" — بانتظار الاعتماد النهائي من المدير",
                    NotificationEventType.TaskStatusChanged,
                    pendingReview.PdfFilePath != null ? $"/DesignProposals/DownloadReview/{pendingReview.Id}" : $"/ProjectTasks/Edit/{task.Id}",
                    entityType: "DesignProposal", entityId: proposal.Id);

                var managerIds = await GetProjectManagersAsync(proposal.ProjectId);
                await _notify.NotifyManyAsync(managerIds,
                    $"توصية المهندس المشرف بالاعتماد على مستند \"{proposal.Name}\" — بانتظار اعتمادك النهائي",
                    NotificationEventType.TaskStatusChanged, $"/ProjectTasks/Edit/{task.Id}",
                    requiresAction: true, entityType: "DesignProposal", entityId: proposal.Id);

                TempData["Success"] = "تم إرسال توصيتك — بانتظار الاعتماد النهائي من المدير";
                return RedirectToAction("Overview", "ProjectAssignments", new { projectId, stageId, taskFilter });
            }

            // قرار نهائي: رفض المشرف في الخطوة الأولى (لا يصل للمدير)، أو اعتماد/رفض المدير في الخطوة الثانية، أو الحالة القديمة بلا مشرف مُعيَّن
            ProposalReview review;
            if (existingReview != null)
            {
                review = existingReview;
                review.Status = status;
                review.ReviewerName = actingUser?.FullName ?? "";
                review.ReviewerPosition = actingUser?.JobRankRef?.NameAr;
                review.ReviewerSignaturePath = actingUser?.SignatureImagePath;
                review.Notes = notes;
                review.AttachmentPath = attachmentPath ?? review.AttachmentPath;
                review.AttachmentFileName = attachmentFileName ?? review.AttachmentFileName;
                review.ReviewDate = DateTime.UtcNow;
                review.ReviewedById = CurrentUserId;
            }
            else
            {
                review = new ProposalReview
                {
                    DesignProposalId = proposal.Id,
                    Status = status,
                    ReviewerName = actingUser?.FullName ?? "",
                    ReviewerPosition = actingUser?.JobRankRef?.NameAr,
                    ReviewerSignaturePath = actingUser?.SignatureImagePath,
                    SupervisorName = hasSupervisor ? actingUser?.FullName : null,
                    SupervisorPosition = hasSupervisor ? actingUser?.JobRankRef?.NameAr : null,
                    SupervisorSignaturePath = hasSupervisor ? actingUser?.SignatureImagePath : null,
                    SupervisorStatus = hasSupervisor ? status : (ProposalStatus?)null,
                    SupervisorReviewedAt = hasSupervisor ? DateTime.UtcNow : (DateTime?)null,
                    Notes = notes,
                    AttachmentPath = attachmentPath,
                    AttachmentFileName = attachmentFileName,
                    Discipline = task.ProjectAssignment?.AssignmentType,
                    ReviewNumber = await ComputeNextReviewNumberAsync(proposal.TaskTodoId),
                    ReviewDate = DateTime.UtcNow,
                    ReviewedById = CurrentUserId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProposalReviews.Add(review);
            }

            proposal.Status = status;
            await _context.SaveChangesAsync();

            if (status == ProposalStatus.Resubmission || status == ProposalStatus.Redesign)
                task.RejectionCount++;

            var previousTaskStatus = task.Status;
            await RecomputeCascadeAsync(todo);

            if (previousTaskStatus != task.Status)
            {
                var workerIds = await GetTaskWorkerIdsAsync(task);
                var message = task.Status == ProjectTaskStatus.Completed
                    ? $"تم اعتماد جميع بنود مهمتك \"{task.Title}\""
                    : (status == ProposalStatus.Resubmission || status == ProposalStatus.Redesign
                        ? $"تحتاج مستنداً معدَّلاً لبند من مهمتك \"{task.Title}\""
                        : $"تم اعتماد بند من مهمتك \"{task.Title}\" — بانتظار بقية البنود");

                foreach (var workerId in workerIds)
                {
                    await _notify.NotifyAsync(workerId, message, NotificationEventType.TaskStatusChanged, $"/ProjectTasks/Edit/{task.Id}",
                        requiresAction: status == ProposalStatus.Resubmission || status == ProposalStatus.Redesign,
                        entityType: "ProjectTask", entityId: task.Id);
                }
            }

            
            var pdfBytes = _pdfService.Generate(review, mainProject, subProject, task.Stage?.Name, proposal.Name, proposal.Code);
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
       NotificationEventType.TaskStatusChanged,
       review.PdfFilePath != null ? $"/DesignProposals/DownloadReview/{review.Id}" : $"/ProjectTasks/Edit/{task.Id}",
       entityType: "DesignProposal", entityId: proposal.Id);

            TempData["Success"] = "تم إرسال المراجعة بنجاح";
            return RedirectToAction("Overview", "ProjectAssignments", new { projectId, stageId, taskFilter });
        }

        // حساب متسلسل: حالة البند من مستنداته ← حالة المهمة من بنودها ← حالة التكليف من مهامه
        private async Task RecomputeCascadeAsync(TaskTodo todo)
        {
            var docs = await _context.DesignProposals.Where(d => d.TaskTodoId == todo.Id).ToListAsync();
            var latestDoc = docs.OrderByDescending(d => d.Revision).FirstOrDefault();
            var todoApproved = latestDoc != null && latestDoc.Status == ProposalStatus.Approved;
            todo.IsCompleted = todoApproved;
            todo.CompletedAt = todoApproved ? (todo.CompletedAt ?? DateTime.UtcNow) : null;
            await _context.SaveChangesAsync();

            await _calc.RecalculateTaskCompletionAsync(todo.TaskId, notifyOnStatusChange: false);

            var task = await _context.ProjectTasks
                .Include(t => t.Todos).ThenInclude(t2 => t2.DesignProposals)
                .FirstOrDefaultAsync(t => t.Id == todo.TaskId);
            if (task == null) return;

            if (task.Status != ProjectTaskStatus.Blocked)
            {
                // إعادة مزامنة كل بنود المهمة حياً من آخر مستند لكل بند، بدل الثقة بالقيمة المخزَّنة (تصحح تلقائياً أي بند شقيق كانت بياناته قديمة)
                bool allTodosApproved = task.Todos.Any();
                foreach (var sibling in task.Todos)
                {
                    var siblingLatestDoc = sibling.DesignProposals.OrderByDescending(d => d.Revision).FirstOrDefault();
                    var siblingApproved = siblingLatestDoc != null && siblingLatestDoc.Status == ProposalStatus.Approved;

                    if (sibling.IsCompleted != siblingApproved)
                    {
                        sibling.IsCompleted = siblingApproved;
                        sibling.CompletedAt = siblingApproved ? (sibling.CompletedAt ?? DateTime.UtcNow) : null;
                    }

                    if (!siblingApproved)
                        allTodosApproved = false;
                }
                await _context.SaveChangesAsync();

                if (allTodosApproved && task.Status != ProjectTaskStatus.Completed)
                {
                    task.Status = ProjectTaskStatus.Completed;
                    await _context.SaveChangesAsync();
                }
                else if (!allTodosApproved && task.Status == ProjectTaskStatus.Completed)
                {
                    task.Status = ProjectTaskStatus.PendingReview;
                    await _context.SaveChangesAsync();
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
                    await _calc.RecalculateAssignmentFinanceAsync(assignment.Id);
                }
            }

            if (task.StageId.HasValue)
                await _calc.RecalculateStageAsync(task.StageId.Value);
        }

        private async Task<int> ComputeNextReviewNumberAsync(int taskTodoId)
        {
            var last = await _context.ProposalReviews
                .Where(r => r.DesignProposal != null && r.DesignProposal.TaskTodoId == taskTodoId)
                .Select(r => (int?)r.ReviewNumber)
                .MaxAsync() ?? 0;
            return last + 1;
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

        [HttpGet]
        public async Task<IActionResult> DownloadReview(int id)
        {
            var review = await _context.ProposalReviews
                .Include(r => r.DesignProposal)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (review == null || string.IsNullOrEmpty(review.PdfFilePath))
                return NotFound();

            if (review.DesignProposal == null || !await _permissionService.CanAccessProjectAsync(User, review.DesignProposal.ProjectId))
                return Forbid();

            var pdfFullPath = Path.Combine(_environment.WebRootPath, review.PdfFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(pdfFullPath))
                return NotFound();

            var reportName = $"مراجعة-{review.DesignProposal?.Code}";

            if (string.IsNullOrEmpty(review.AttachmentPath))
            {
                return PhysicalFile(pdfFullPath, "application/pdf", $"{reportName}.pdf");
            }

            var attachmentFullPath = Path.Combine(_environment.WebRootPath, review.AttachmentPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            using var memoryStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                archive.CreateEntryFromFile(pdfFullPath, $"{reportName}.pdf");
                if (System.IO.File.Exists(attachmentFullPath))
                {
                    var attachmentName = !string.IsNullOrEmpty(review.AttachmentFileName) ? review.AttachmentFileName : Path.GetFileName(attachmentFullPath);
                    archive.CreateEntryFromFile(attachmentFullPath, attachmentName);
                }
            }
            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "application/zip", $"{reportName}.zip");
        }
    }
}