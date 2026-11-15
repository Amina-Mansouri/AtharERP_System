using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    public class PerformanceEvaluationExportService
    {
        private readonly AppDbContext _context;
        private readonly ClassificationValidationService _validationService;
        private readonly IWebHostEnvironment _environment;

        public PerformanceEvaluationExportService(AppDbContext context, ClassificationValidationService validationService, IWebHostEnvironment environment)
        {
            _context = context;
            _validationService = validationService;
            _environment = environment;
        }

        public class ExportResult
        {
            public bool Success { get; set; }
            public byte[]? FileBytes { get; set; }
            public List<string> BlockingErrors { get; set; } = new();
            public int SoftWarningCount { get; set; }
        }

        // صف واحد = نسخة واحدة من مقترح تصميمي — يمثّل سطر TrackingTable، على مستوى المهمة (مو المشروع)
        private class TrackingRow
        {
            public string ProjectNumber = "";
            public string Discipline = "";
            public string BuildingNumber = "";
            public string BuildingName = "";
            public string Priority = "";
            public string Status = "";
            public string EmployeeName = "";
            public string EmployeeEmail = "";
            public string Scope = "";
            public string Element = "";
            public int RevisionCount;
            public string RevisionReason = "";
            public decimal? Area;
            public DateTime? RequestDate;
            public DateTime? StartDate;
            public DateTime? DueDate;
            public DateTime? ActualDate;
            public int? DurationDays;
            public int? RemainingDays;
            public decimal? CompletionPercentage;
            public string DeadlineStatus = "";
            public DateTime? LastUpdated;
        }

        public async Task<ExportResult> GenerateAsync(int year, int month)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var today = DateTime.UtcNow.Date;

            var proposals = await _context.DesignProposals
                .Include(p => p.Project).ThenInclude(pr => pr.ParentProject)
                .Include(p => p.Project).ThenInclude(pr => pr.ProjectCategory)
                .Include(p => p.PreparedBy).ThenInclude(u => u!.Department)
                .Include(p => p.TaskTodo).ThenInclude(t => t.Task).ThenInclude(t => t!.Stage)
                .Include(p => p.TaskTodo).ThenInclude(t => t.Task).ThenInclude(t => t!.ProjectAssignment)
                .Where(p => p.SubmittedDate != null && p.SubmittedDate >= monthStart && p.SubmittedDate <= monthEnd)
                .ToListAsync();

            if (!proposals.Any())
                return new ExportResult { Success = false, BlockingErrors = { "لا توجد أي نسخ مُقدَّمة خلال هذا الشهر" } };

            // نحتاج مراجعة النسخة السابقة (لو وُجدت) لمعرفة سبب النسخة وتاريخ بدايتها — بغض النظر متى قُدِّمت هي
            var taskTodoIds = proposals.Select(p => p.TaskTodoId).Distinct().ToList();
            var allRelatedProposals = await _context.DesignProposals
                .Include(p => p.Reviews)
                .Where(p => taskTodoIds.Contains(p.TaskTodoId))
                .ToListAsync();

            var rows = new List<TrackingRow>();
            var projectMasterEntries = new Dictionary<int, (string Code, string Name, string? Category)>();

            foreach (var p in proposals)
            {
                var task = p.TaskTodo.Task;
                var project = p.Project;
                var isSub = project.Scope == ProjectScope.Sub;
                var mainProject = isSub ? (project.ParentProject ?? project) : project;

                if (!projectMasterEntries.ContainsKey(mainProject.Id))
                    projectMasterEntries[mainProject.Id] = (mainProject.Code, mainProject.Name, mainProject.ProjectCategory?.Classification);

                DateTime? startDate = task.PlannedStartDate;
                string revisionReason = "";
                if (p.Revision > 1)
                {
                    var previous = allRelatedProposals.FirstOrDefault(x => x.TaskTodoId == p.TaskTodoId && x.Revision == p.Revision - 1);
                    var previousReview = previous?.Reviews.OrderByDescending(r => r.ReviewDate).FirstOrDefault();
                    if (previousReview != null)
                    {
                        startDate = previousReview.ReviewDate;
                        revisionReason = EnumDisplayHelper.GetDisplayList<ProposalStatus>()
                            .FirstOrDefault(x => x.Value == previousReview.Status.ToString()).Label ?? "";
                    }
                }

                var dueDate = task.PlannedEndDate;
                var actualDate = p.SubmittedDate;

                int? durationDays = (startDate.HasValue && dueDate.HasValue) ? (dueDate.Value.Date - startDate.Value.Date).Days : null;
                int? remainingDays = dueDate.HasValue ? (dueDate.Value.Date - today).Days : null;
                string deadlineStatus = "";
                if (actualDate.HasValue && dueDate.HasValue)
                    deadlineStatus = actualDate.Value.Date < dueDate.Value.Date ? "Early" : (actualDate.Value.Date == dueDate.Value.Date ? "On Time" : "Late");

                rows.Add(new TrackingRow
                {
                    ProjectNumber = mainProject.Code,
                    Discipline = p.PreparedBy?.Department?.Name ?? "",
                    BuildingNumber = isSub ? project.Code : "",
                    BuildingName = isSub ? project.Name : "",
                    Priority = EnumDisplayHelper.GetDisplayList<TaskPriority>().FirstOrDefault(x => x.Value == task.Priority.ToString()).Label ?? "",
                    Status = EnumDisplayHelper.GetDisplayList<ProjectStatus>().FirstOrDefault(x => x.Value == mainProject.Status.ToString()).Label ?? "",
                    EmployeeName = p.PreparedBy?.FullName ?? "",
                    EmployeeEmail = p.PreparedBy?.Email ?? "",
                    Scope = task.Stage?.Name ?? "",
                    Element = task.ProjectAssignment?.AssignmentType ?? "",
                    RevisionCount = p.Revision,
                    RevisionReason = revisionReason,
                    Area = task.ProjectAssignment?.Area,
                    RequestDate = mainProject.CreatedAt,
                    StartDate = startDate,
                    DueDate = dueDate,
                    ActualDate = actualDate,
                    DurationDays = durationDays,
                    RemainingDays = remainingDays,
                    CompletionPercentage = task.CompletionPercentage,
                    DeadlineStatus = deadlineStatus,
                    LastUpdated = actualDate
                });
            }

            var blockingErrors = await _validationService.ValidateAsync(
                rows.Select(r => r.Discipline),
                rows.Select(r => r.Priority),
                rows.Where(r => !string.IsNullOrEmpty(r.RevisionReason)).Select(r => r.RevisionReason),
                rows.Select(r => projectMasterEntries.Values.FirstOrDefault(v => v.Code == r.ProjectNumber).Category ?? ""));

            if (blockingErrors.Any())
                return new ExportResult { Success = false, BlockingErrors = blockingErrors };

            var templatePath = Path.Combine(_environment.WebRootPath, "templates", "kpi-performance-template.xlsx");
            using var workbook = new XLWorkbook(templatePath);

            var scopesSheet = workbook.Worksheet("scopes&elements");
            var validScopes = scopesSheet.Column(1).CellsUsed().Skip(1).Select(c => c.GetString().Trim()).ToHashSet();
            var validElements = scopesSheet.Column(3).CellsUsed().Skip(1).Select(c => c.GetString().Trim()).ToHashSet();

            var pmSheet = workbook.Worksheet("Project Master");
            int pmRow = 2;
            foreach (var entry in projectMasterEntries.Values)
            {
                pmSheet.Cell(pmRow, 1).Value = entry.Code;
                pmSheet.Cell(pmRow, 2).Value = entry.Name;
                pmSheet.Cell(pmRow, 3).Value = entry.Category ?? "";
                pmRow++;
            }

            var ttSheet = workbook.Worksheet("TrackingTable");
            int ttRow = 2;
            int softWarnings = 0;
            foreach (var r in rows)
            {
                ttSheet.Cell(ttRow, 1).Value = r.ProjectNumber;
                ttSheet.Cell(ttRow, 4).Value = r.Discipline;
                ttSheet.Cell(ttRow, 5).Value = r.BuildingNumber;
                ttSheet.Cell(ttRow, 6).Value = r.BuildingName;
                ttSheet.Cell(ttRow, 7).Value = r.Priority;
                ttSheet.Cell(ttRow, 8).Value = r.Status;
                ttSheet.Cell(ttRow, 9).Value = r.EmployeeName;
                ttSheet.Cell(ttRow, 10).Value = r.EmployeeEmail;

                var scopeCell = ttSheet.Cell(ttRow, 11);
                scopeCell.Value = r.Scope;
                if (!string.IsNullOrEmpty(r.Scope) && !validScopes.Contains(r.Scope))
                {
                    scopeCell.Style.Fill.BackgroundColor = XLColor.Red;
                    scopeCell.Style.Font.FontColor = XLColor.White;
                    softWarnings++;
                }

                var elementCell = ttSheet.Cell(ttRow, 12);
                elementCell.Value = r.Element;
                if (!string.IsNullOrEmpty(r.Element) && !validElements.Contains(r.Element))
                {
                    elementCell.Style.Fill.BackgroundColor = XLColor.Red;
                    elementCell.Style.Font.FontColor = XLColor.White;
                    softWarnings++;
                }

                ttSheet.Cell(ttRow, 13).Value = r.RevisionCount;
                ttSheet.Cell(ttRow, 14).Value = r.RevisionReason;
                if (r.Area.HasValue) ttSheet.Cell(ttRow, 15).Value = r.Area.Value;
                if (r.RequestDate.HasValue) ttSheet.Cell(ttRow, 16).Value = r.RequestDate.Value;
                if (r.StartDate.HasValue) ttSheet.Cell(ttRow, 17).Value = r.StartDate.Value;
                if (r.DueDate.HasValue) ttSheet.Cell(ttRow, 18).Value = r.DueDate.Value;
                if (r.ActualDate.HasValue) ttSheet.Cell(ttRow, 19).Value = r.ActualDate.Value;
                if (r.DurationDays.HasValue) ttSheet.Cell(ttRow, 20).Value = r.DurationDays.Value;
                if (r.RemainingDays.HasValue) ttSheet.Cell(ttRow, 21).Value = r.RemainingDays.Value;
                if (r.CompletionPercentage.HasValue) ttSheet.Cell(ttRow, 22).Value = r.CompletionPercentage.Value;
                ttSheet.Cell(ttRow, 23).Value = r.DeadlineStatus;
                if (r.LastUpdated.HasValue) ttSheet.Cell(ttRow, 24).Value = r.LastUpdated.Value;

                ttRow++;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);

            return new ExportResult { Success = true, FileBytes = ms.ToArray(), SoftWarningCount = softWarnings };
        }
    }
}