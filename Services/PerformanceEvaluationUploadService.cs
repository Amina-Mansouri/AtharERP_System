using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    public class PerformanceEvaluationUploadService
    {
        private readonly AppDbContext _context;
        private readonly ClassificationValidationService _validationService;
        private readonly FileUploadService _fileUpload;

        public PerformanceEvaluationUploadService(AppDbContext context, ClassificationValidationService validationService, FileUploadService fileUpload)
        {
            _context = context;
            _validationService = validationService;
            _fileUpload = fileUpload;
        }

        public class UploadResult
        {
            public bool Success { get; set; }
            public List<string> Errors { get; set; } = new();
            public int EmployeesProcessed { get; set; }
        }

        public async Task<UploadResult> ProcessUploadAsync(byte[] fileBytes, int year, int month, string uploadedById)
        {
            var errors = new List<string>();
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var alreadyUploaded = await _context.PerformanceEvaluationUploads
                .AnyAsync(u => u.Year == year && u.Month == month && u.ValidationPassed);
            if (alreadyUploaded)
                return new UploadResult { Success = false, Errors = { $"يوجد بالفعل ملف مقبول لشهر {month}/{year} — ارفعي ملفاً لشهر آخر أو احذفي الرفعة القديمة أولاً" } };

            using var ms = new MemoryStream(fileBytes);
            using var workbook = new XLWorkbook(ms);

            // ===== التحقق من الفترة =====
            var summarySheet = workbook.Worksheet("Employee KPI Summary");
            var reportStart = summarySheet.Cell(2, 2).GetDateTime();
            var reportEnd = summarySheet.Cell(2, 4).GetDateTime();
            if (reportStart.Date != monthStart.Date || reportEnd.Date != monthEnd.Date)
                errors.Add($"فترة الملف ({reportStart:yyyy-MM-dd} إلى {reportEnd:yyyy-MM-dd}) لا تطابق الشهر المطلوب ({monthStart:yyyy-MM-dd} إلى {monthEnd:yyyy-MM-dd})");

            // ===== التحقق من التصنيفات =====
            var trackingSheet = workbook.Worksheet("TrackingTable");
            var usedRows = trackingSheet.RowsUsed().Skip(1).ToList();
            var disciplines = usedRows.Select(r => r.Cell(4).GetString()).ToList();
            var priorities = usedRows.Select(r => r.Cell(7).GetString()).ToList();
            var revisionReasons = usedRows.Select(r => r.Cell(14).GetString()).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            var projectTypes = usedRows.Select(r => r.Cell(3).GetString()).ToList();

            errors.AddRange(await _validationService.ValidateAsync(disciplines, priorities, revisionReasons, projectTypes));

            if (errors.Any())
                return new UploadResult { Success = false, Errors = errors };

            // ===== استخراج Employee KPI Summary (من الصف 5) =====
            var employeeKpis = new List<EmployeeMonthlyKpi>();
            var nameToUserId = new Dictionary<string, string>();
            var summaryRows = summarySheet.RowsUsed().Skip(4);
            foreach (var row in summaryRows)
            {
                var email = row.Cell(2).GetString().Trim();
                if (string.IsNullOrWhiteSpace(email)) continue;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    errors.Add($"لم يتم العثور على موظفة بالبريد \"{email}\" بشيت Employee KPI Summary");
                    continue;
                }

                var name = row.Cell(1).GetString().Trim();
                if (!string.IsNullOrEmpty(name)) nameToUserId[name] = user.Id;

                employeeKpis.Add(new EmployeeMonthlyKpi
                {
                    UserId = user.Id,
                    Discipline = row.Cell(3).GetString(),
                    TeamDepartment = row.Cell(4).GetString(),
                    StandardWorkingHours = row.Cell(5).TryGetValue<decimal>(out var v5) ? v5 : null,
                    ApprovedLeaveHours = row.Cell(6).TryGetValue<decimal>(out var v6) ? v6 : null,
                    AvailableWorkingHours = row.Cell(7).TryGetValue<decimal>(out var v7) ? v7 : null,
                    ActualWorkingHours = row.Cell(8).TryGetValue<decimal>(out var v8) ? v8 : null,
                    WorkingHoursUtilizationPercent = row.Cell(9).TryGetValue<decimal>(out var v9) ? v9 : null,
                    StandardProductionTarget = row.Cell(10).TryGetValue<decimal>(out var v10) ? v10 : null,
                    AdjustedProductionTarget = row.Cell(11).TryGetValue<decimal>(out var v11) ? v11 : null,
                    TotalBaseProduction = row.Cell(12).TryGetValue<decimal>(out var v12) ? v12 : null,
                    RecognizedProductionWithinPeriod = row.Cell(13).TryGetValue<decimal>(out var v13) ? v13 : null,
                    ProductionAchievementPercent = row.Cell(14).TryGetValue<decimal>(out var v14) ? v14 : null,
                    TotalRevisionDeduction = row.Cell(15).TryGetValue<decimal>(out var v15) ? v15 : null,
                    TotalFinalProductionScore = row.Cell(16).TryGetValue<decimal>(out var v16) ? v16 : null,
                    OnTimeTasks = row.Cell(17).TryGetValue<int>(out var v17) ? v17 : null,
                    EarlyTasks = row.Cell(18).TryGetValue<int>(out var v18) ? v18 : null,
                    LateTasks = row.Cell(19).TryGetValue<int>(out var v19) ? v19 : null,
                    OnTimeDeliveryRatePercent = row.Cell(20).TryGetValue<decimal>(out var v20) ? v20 : null,
                    TotalTasks = row.Cell(21).TryGetValue<int>(out var v21) ? v21 : null,
                    CompletedTasks = row.Cell(22).TryGetValue<int>(out var v22) ? v22 : null,
                    PendingTasks = row.Cell(23).TryGetValue<int>(out var v23) ? v23 : null,
                    EngineeringKpi = row.Cell(24).TryGetValue<decimal>(out var v24) ? v24 : null,
                    HrEvaluation = row.Cell(25).TryGetValue<decimal>(out var v25) ? v25 : null,
                    EngineeringKpiWeight = row.Cell(26).TryGetValue<decimal>(out var v26) ? v26 : null,
                    HrEvaluationWeight = row.Cell(27).TryGetValue<decimal>(out var v27) ? v27 : null,
                    FinalMonthlyKpi = row.Cell(28).TryGetValue<decimal>(out var v28) ? v28 : null,
                    PerformanceGrade = row.Cell(29).GetString()
                });
            }

            if (errors.Any())
                return new UploadResult { Success = false, Errors = errors };

            // ===== استخراج تفاصيل المهام (kpi calculations، من الصف 2) =====
            var taskDetails = new List<PerformanceTaskDetail>();
            var kpiSheet = workbook.Worksheet("kpi calculations");
            foreach (var row in kpiSheet.RowsUsed().Skip(1))
            {
                var employeeName = row.Cell(9).GetString().Trim();
                if (string.IsNullOrEmpty(employeeName) || !nameToUserId.TryGetValue(employeeName, out var userId))
                    continue;

                taskDetails.Add(new PerformanceTaskDetail
                {
                    UserId = userId,
                    ProjectNumber = row.Cell(1).GetString(),
                    ProjectName = row.Cell(2).GetString(),
                    ProjectType = row.Cell(3).GetString(),
                    Discipline = row.Cell(4).GetString(),
                    BuildingNumber = row.Cell(5).GetString(),
                    BuildingName = row.Cell(6).GetString(),
                    Priority = row.Cell(7).GetString(),
                    TaskStatus = row.Cell(8).GetString(),
                    Scope = row.Cell(10).GetString(),
                    Element = row.Cell(11).GetString(),
                    BuildingArea = row.Cell(12).TryGetValue<decimal>(out var a1) ? a1 : null,
                    ProjectTypeFactor = row.Cell(13).TryGetValue<decimal>(out var a2) ? a2 : null,
                    ScopeWeight = row.Cell(14).TryGetValue<decimal>(out var a3) ? a3 : null,
                    ElementWeight = row.Cell(15).TryGetValue<decimal>(out var a4) ? a4 : null,
                    BaseProduction = row.Cell(16).TryGetValue<decimal>(out var a5) ? a5 : null,
                    DeadlineStatus = row.Cell(17).GetString(),
                    DeadlineFactor = row.Cell(18).TryGetValue<decimal>(out var a6) ? a6 : null,
                    RevisionCount = row.Cell(19).TryGetValue<int>(out var a7) ? a7 : null,
                    RevisionReason = row.Cell(20).GetString(),
                    EligibleRevisions = row.Cell(21).TryGetValue<int>(out var a8) ? a8 : null,
                    EligibilityReduction = row.Cell(22).TryGetValue<decimal>(out var a9) ? a9 : null,
                    RevisionCompliance = row.Cell(23).TryGetValue<decimal>(out var a10) ? a10 : null,
                    FinalTaskScore = row.Cell(24).TryGetValue<decimal>(out var a11) ? a11 : null,
                    TaskStartDate = row.Cell(25).TryGetValue<DateTime>(out var a12) ? a12 : null,
                    TaskDueDate = row.Cell(26).TryGetValue<DateTime>(out var a13) ? a13 : null,
                    TotalTaskDays = row.Cell(27).TryGetValue<int>(out var a14) ? a14 : null,
                    DailyBaseProduction = row.Cell(28).TryGetValue<decimal>(out var a15) ? a15 : null,
                    DaysWithinReportPeriod = row.Cell(29).TryGetValue<int>(out var a16) ? a16 : null,
                    RecognizedProductionWithinPeriod = row.Cell(30).TryGetValue<decimal>(out var a17) ? a17 : null
                });
            }

            // ===== الأرشفة والحفظ =====
            var saveResult = await _fileUpload.SaveGeneratedFileAsync(fileBytes, $"performance-evaluation/{year}-{month:D2}", ".xlsx");

            var upload = new PerformanceEvaluationUpload
            {
                Year = year,
                Month = month,
                FilePath = saveResult.FilePath,
                ValidationPassed = true,
                UploadedById = uploadedById,
                UploadedAt = DateTime.UtcNow
            };
            foreach (var e in employeeKpis) upload.EmployeeResults.Add(e);

            _context.PerformanceEvaluationUploads.Add(upload);
            await _context.SaveChangesAsync();

            foreach (var t in taskDetails) t.PerformanceEvaluationUploadId = upload.Id;
            _context.PerformanceTaskDetails.AddRange(taskDetails);
            await _context.SaveChangesAsync();

            return new UploadResult { Success = true, EmployeesProcessed = employeeKpis.Count };
        }
    }
}