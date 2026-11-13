using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class ProjectReportsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly NotificationService _notify;
        private readonly AuditService _audit;
        private readonly FileUploadService _fileUpload;
        private readonly ProjectReportsPdfService _pdfService;
        private readonly IWebHostEnvironment _environment;

        public ProjectReportsController(AppDbContext context, PermissionService permissionService, NotificationService notify, AuditService audit, FileUploadService fileUpload, ProjectReportsPdfService pdfService, IWebHostEnvironment environment)
        {
            _context = context;
            _permissionService = permissionService;
            _notify = notify;
            _audit = audit;
            _fileUpload = fileUpload;
            _pdfService = pdfService;
            _environment = environment;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task ArchiveClosurePdfAsync(int reportId)
        {
            var full = await _context.ProjectClosureReports
                .Include(r => r.Project)
                .Include(r => r.CreatedBy)
                .Include(r => r.DecidedBy)
                .Include(r => r.Stages).ThenInclude(s => s.Stage)
                .FirstAsync(r => r.Id == reportId);

            var site = await _context.Sites.FirstOrDefaultAsync(s => s.ProjectId == full.ProjectId);
            var revisions = await _context.DocumentRevisions
                .Include(rv => rv.Document)
                .Where(rv => rv.Document.ProjectId == full.ProjectId && rv.Status == RevisionStatus.Approved)
                .ToListAsync();

            var bytes = _pdfService.GenerateClosurePdf(full, site, revisions);
            if (!string.IsNullOrEmpty(full.PdfFilePath))
                _fileUpload.DeleteFile(full.PdfFilePath);

            var result = await _fileUpload.SaveGeneratedFileAsync(bytes, $"project-closure-reports/{full.Id}", ".pdf");
            if (result.Success)
            {
                full.PdfFilePath = result.FilePath;
                await _context.SaveChangesAsync();
            }
        }

        private async Task ArchiveDeliveryPdfAsync(int reportId)
        {
            var full = await _context.ProjectDeliveryReports
                .Include(r => r.Project)
                .Include(r => r.Stage)
                .Include(r => r.CreatedBy)
                .Include(r => r.DecidedBy)
                .Include(r => r.DeliveredFiles).ThenInclude(f => f.DocumentRevision).ThenInclude(rv => rv.Document)
                .FirstAsync(r => r.Id == reportId);

            var site = await _context.Sites.FirstOrDefaultAsync(s => s.ProjectId == full.ProjectId);

            var linkedAssignments = await _context.ProjectAssignments
     .Include(a => a.Engineers).ThenInclude(e => e.User)
     .Where(a => a.StageId == full.ProjectStageId)
     .ToListAsync();
            var incompleteAssignments = linkedAssignments
                .Where(a => a.Status != AssignmentStatus.Completed && a.Status != AssignmentStatus.Cancelled)
                .ToList();

            var bytes = _pdfService.GenerateDeliveryPdf(full, site, linkedAssignments, incompleteAssignments);
            if (!string.IsNullOrEmpty(full.PdfFilePath))
                _fileUpload.DeleteFile(full.PdfFilePath);

            var result = await _fileUpload.SaveGeneratedFileAsync(bytes, $"project-delivery-reports/{full.Id}", ".pdf");
            if (result.Success)
            {
                full.PdfFilePath = result.FilePath;
                await _context.SaveChangesAsync();
            }
        }

        [Authorize]
        public async Task<IActionResult> Index(int? mainProjectId, int? projectId)
        {
            bool canCreate = await _permissionService.HasPermissionAsync(User, "Projects.Reports.Create");
            bool canApprove = await _permissionService.HasPermissionAsync(User, "Projects.Reports.Approve");
            ViewBag.CanCreate = canCreate;
            ViewBag.CanApprove = canApprove;

            ViewBag.MainProjects = await _context.Projects
                .Where(p => p.Scope == ProjectScope.Main)
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewBag.SelectedMainProjectId = mainProjectId;
            ViewBag.SelectedProjectId = projectId;

            if (mainProjectId.HasValue)
            {
                ViewBag.MainProjectOption = await _context.Projects.FirstOrDefaultAsync(p => p.Id == mainProjectId.Value);
                ViewBag.SubProjects = await _context.Projects
                    .Where(p => p.ParentProjectId == mainProjectId.Value)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }

            if (canCreate)
            {
                ViewBag.MyClosureReports = await _context.ProjectClosureReports
                    .Include(r => r.Project)
                    .Where(r => r.CreatedById == CurrentUserId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
                ViewBag.MyDeliveryReports = await _context.ProjectDeliveryReports
                    .Include(r => r.Project)
                    .Where(r => r.CreatedById == CurrentUserId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }

            if (canApprove)
            {
                ViewBag.PendingClosureReports = await _context.ProjectClosureReports
                    .Include(r => r.Project)
                    .Include(r => r.CreatedBy)
                    .Where(r => r.Status == ClosureReportStatus.Pending)
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync();
                ViewBag.PendingDeliveryReports = await _context.ProjectDeliveryReports
                    .Include(r => r.Project)
                    .Include(r => r.CreatedBy)
                    .Where(r => r.Status == DeliveryReportStatus.Pending)
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync();
            }

            return View();
        }

        [RequirePermission("Projects.Reports.Create")]
        public async Task<IActionResult> CreateClosure(int projectId)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound();

            ViewBag.Project = project;
            ViewBag.Stages = await _context.ProjectStages
                .Where(s => s.ProjectId == projectId)
                .OrderBy(s => s.Sequence)
                .ToListAsync();
            return View();
        }

        [RequirePermission("Projects.Reports.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClosure(int projectId, List<int> stageIds, DateTime? closureDate,
            bool scopeFullyCompleted, bool scopePartiallyWithNotes, bool scopeExcludedWorkExists, string? scopeNotes)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound();

            if (stageIds == null || !stageIds.Any())
            {
                TempData["Error"] = "يجب اختيار مرحلة واحدة على الأقل";
                return RedirectToAction("CreateClosure", new { projectId });
            }

            var closureCount = await _context.ProjectClosureReports.CountAsync(r => r.ProjectId == projectId);
            var code = $"CLR-{project.Code}-{closureCount + 1:D3}";
            var version = (closureCount + 1).ToString();

            var report = new ProjectClosureReport
            {
                ProjectId = projectId,
                ReportCode = code,
                Version = version,
                ClosureDate = closureDate,
                ScopeFullyCompleted = scopeFullyCompleted,
                ScopePartiallyWithNotes = scopePartiallyWithNotes,
                ScopeExcludedWorkExists = scopeExcludedWorkExists,
                ScopeNotes = scopeNotes,
                Status = ClosureReportStatus.Pending,
                CreatedById = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };
            foreach (var sid in stageIds.Distinct())
                report.Stages.Add(new ProjectClosureReportStage { ProjectStageId = sid });

            _context.ProjectClosureReports.Add(report);
            await _context.SaveChangesAsync();
            await ArchiveClosurePdfAsync(report.Id);

            var me = await _context.Users.FindAsync(CurrentUserId);
            var approverIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("Projects.Reports.Approve");
            await _notify.NotifyManyAsync(approverIds, $"تقرير إغلاق مشروع جديد: {project.Name}", NotificationEventType.ProjectClosureReportCreated, "/ProjectReports/Index", requiresAction: true, entityType: "ProjectClosureReport", entityId: report.Id);

            TempData["Success"] = "تم إرسال تقرير الإغلاق بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("Projects.Reports.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideClosure(int id, bool approved, ClosureFinalStatus? finalStatus, string? finalStatusNotes)
        {
            var report = await _context.ProjectClosureReports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();

            if (report.Status != ClosureReportStatus.Pending)
            {
                TempData["Error"] = "تم اتخاذ قرار بشأن هذا التقرير مسبقاً";
                return RedirectToAction("Index");
            }

            if (approved && finalStatus == null)
            {
                TempData["Error"] = "يجب تحديد حالة المشروع النهائية عند الاعتماد";
                return RedirectToAction("Index");
            }

            report.Status = approved ? ClosureReportStatus.Approved : ClosureReportStatus.Rejected;
            report.DecidedById = CurrentUserId;
            report.DecidedAt = DateTime.UtcNow;
            if (approved)
            {
                report.FinalStatus = finalStatus;
                report.FinalStatusNotes = finalStatusNotes;
            }
            await _context.SaveChangesAsync();
            await ArchiveClosurePdfAsync(report.Id);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ProjectClosureReport), report.Id.ToString(), approved ? "اعتماد تقرير إغلاق مشروع" : "رفض تقرير إغلاق مشروع");
            await _notify.NotifyAsync(report.CreatedById, approved ? "تم اعتماد تقرير إغلاق المشروع" : "تم رفض تقرير إغلاق المشروع", NotificationEventType.ProjectClosureReportDecided, "/ProjectReports/Index", entityType: "ProjectClosureReport", entityId: report.Id);

            TempData["Success"] = "تم تسجيل القرار";
            return RedirectToAction("Index");
        }

        [RequirePermission("Projects.Reports.Create")]
        public async Task<IActionResult> CreateDelivery(int projectId, int? stageId)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound();

            ViewBag.Project = project;
            ViewBag.Stages = await _context.ProjectStages
                .Where(s => s.ProjectId == projectId)
                .OrderBy(s => s.Sequence)
                .ToListAsync();

            if (stageId.HasValue)
            {
                var linkedAssignments = await _context.ProjectAssignments
                    .Include(a => a.Engineers).ThenInclude(e => e.User)
                    .Where(a => a.StageId == stageId.Value)
                    .ToListAsync();
                ViewBag.SelectedStageId = stageId.Value;
                ViewBag.LinkedAssignments = linkedAssignments;
                ViewBag.IncompleteAssignments = linkedAssignments
                    .Where(a => a.Status != AssignmentStatus.Completed && a.Status != AssignmentStatus.Cancelled)
                    .ToList();
                ViewBag.CandidateRevisions = await _context.DocumentRevisions
                    .Include(rv => rv.Document)
                    .Where(rv => rv.Document.ProjectId == projectId && rv.Status == RevisionStatus.Approved)
                    .ToListAsync();
            }

            return View();
        }

        [RequirePermission("Projects.Reports.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDelivery(int projectId, int stageId, List<int>? deliveredRevisionIds, string? notes)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound();

            var linkedAssignments = await _context.ProjectAssignments
                .Where(a => a.StageId == stageId)
                .ToListAsync();
            var starts = linkedAssignments.Where(a => a.PlannedStartDate.HasValue).Select(a => a.PlannedStartDate!.Value).ToList();
            var ends = linkedAssignments.Where(a => a.PlannedEndDate.HasValue).Select(a => a.PlannedEndDate!.Value).ToList();

            var code = $"DLR-{project.Code}-{(await _context.ProjectDeliveryReports.CountAsync(r => r.ProjectId == projectId)) + 1:D3}";

            var report = new ProjectDeliveryReport
            {
                ProjectId = projectId,
                ProjectStageId = stageId,
                ReportCode = code,
                AssignmentDate = starts.Any() ? starts.Min() : (DateTime?)null,
                ActualDeliveryDate = ends.Any() ? ends.Max() : (DateTime?)null,
                Notes = notes,
                Status = DeliveryReportStatus.Pending,
                CreatedById = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };

            if (deliveredRevisionIds != null)
                foreach (var rid in deliveredRevisionIds.Distinct())
                    report.DeliveredFiles.Add(new ProjectDeliveryReportFile { DocumentRevisionId = rid });

            _context.ProjectDeliveryReports.Add(report);
            await _context.SaveChangesAsync();
            await ArchiveDeliveryPdfAsync(report.Id);

            var me = await _context.Users.FindAsync(CurrentUserId);
            var approverIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("Projects.Reports.Approve");
            await _notify.NotifyManyAsync(approverIds, $"نموذج تسليم مشروع جديد: {project.Name}", NotificationEventType.ProjectDeliveryReportCreated, "/ProjectReports/Index", requiresAction: true, entityType: "ProjectDeliveryReport", entityId: report.Id);

            TempData["Success"] = "تم إرسال نموذج التسليم بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("Projects.Reports.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideDelivery(int id, bool approved)
        {
            var report = await _context.ProjectDeliveryReports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();

            if (report.Status != DeliveryReportStatus.Pending)
            {
                TempData["Error"] = "تم اتخاذ قرار بشأن هذا النموذج مسبقاً";
                return RedirectToAction("Index");
            }

            report.Status = approved ? DeliveryReportStatus.Approved : DeliveryReportStatus.Rejected;
            report.DecidedById = CurrentUserId;
            report.DecidedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await ArchiveDeliveryPdfAsync(report.Id);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(ProjectDeliveryReport), report.Id.ToString(), approved ? "اعتماد نموذج تسليم مشروع" : "رفض نموذج تسليم مشروع");
            await _notify.NotifyAsync(report.CreatedById, approved ? "تم اعتماد نموذج تسليم المشروع" : "تم رفض نموذج تسليم المشروع", NotificationEventType.ProjectDeliveryReportDecided, "/ProjectReports/Index", entityType: "ProjectDeliveryReport", entityId: report.Id);

            TempData["Success"] = "تم تسجيل القرار";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> DownloadClosure(int id)
        {
            var report = await _context.ProjectClosureReports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null || string.IsNullOrEmpty(report.PdfFilePath)) return NotFound();

            bool canApprove = await _permissionService.HasPermissionAsync(User, "Projects.Reports.Approve");
            if (report.CreatedById != CurrentUserId && !canApprove) return Forbid();

            var fullPath = Path.Combine(_environment.WebRootPath, report.PdfFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return NotFound();
            return PhysicalFile(fullPath, "application/pdf", $"تقرير-إغلاق-{report.Id}.pdf");
        }

        [Authorize]
        public async Task<IActionResult> DownloadDelivery(int id)
        {
            var report = await _context.ProjectDeliveryReports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null || string.IsNullOrEmpty(report.PdfFilePath)) return NotFound();

            bool canApprove = await _permissionService.HasPermissionAsync(User, "Projects.Reports.Approve");
            if (report.CreatedById != CurrentUserId && !canApprove) return Forbid();

            var fullPath = Path.Combine(_environment.WebRootPath, report.PdfFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return NotFound();
            return PhysicalFile(fullPath, "application/pdf", $"نموذج-تسليم-{report.Id}.pdf");
        }
    }
}