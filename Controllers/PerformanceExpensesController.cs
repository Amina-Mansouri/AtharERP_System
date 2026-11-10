using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class PerformanceExpensesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly NotificationService _notify;

        public PerformanceExpensesController(AppDbContext context, AuditService audit, NotificationService notify)
        {
            _context = context;
            _audit = audit;
            _notify = notify;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("Finance.Costs.View")]
        public async Task<IActionResult> Index(string? engineerId, List<int>? projectIds)
        {
            ViewBag.Engineers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
            ViewBag.EngineerId = engineerId;
            ViewBag.SelectedProjectIds = projectIds ?? new List<int>();

            if (string.IsNullOrEmpty(engineerId))
                return View();

            var engineer = await _context.Users.FindAsync(engineerId);
            if (engineer == null)
                return NotFound();
            ViewBag.Engineer = engineer;

            var myProjectIds = await _context.ProjectAssignments
                .Where(a => a.Engineers.Any(e => e.UserId == engineerId))
                .Select(a => a.ProjectId)
                .Distinct()
                .ToListAsync();
            ViewBag.AvailableProjects = await _context.Projects.Where(p => myProjectIds.Contains(p.Id)).OrderBy(p => p.Code).ToListAsync();

            if (projectIds == null || !projectIds.Any())
                return View();

            var assignments = await _context.ProjectAssignments
                .Include(a => a.Project)
                .Where(a => a.Engineers.Any(e => e.UserId == engineerId) && projectIds.Contains(a.ProjectId))
                .ToListAsync();

            if (!assignments.Any())
            {
                ViewBag.NoAssignments = true;
                return View();
            }

            decimal totalSales = assignments.Sum(a => a.AssignmentSaleValue);

            var periodStart = assignments.Where(a => a.PlannedStartDate.HasValue).Select(a => a.PlannedStartDate!.Value).DefaultIfEmpty(DateTime.UtcNow.AddMonths(-1)).Min();
            var periodEnd = assignments.Select(a => a.PlannedEndDate ?? DateTime.UtcNow).DefaultIfEmpty(DateTime.UtcNow).Max();
            if (periodEnd > DateTime.UtcNow) periodEnd = DateTime.UtcNow;
            if (periodEnd < periodStart) periodEnd = periodStart;

            var salaryPayments = await _context.SalaryPayments.Where(s => s.UserId == engineerId).ToListAsync();
            var periodStartMonth = new DateTime(periodStart.Year, periodStart.Month, 1);
            var periodEndMonth = new DateTime(periodEnd.Year, periodEnd.Month, 1);
            decimal totalSalaryInPeriod = salaryPayments
                .Where(s => new DateTime(s.Year, s.Month, 1) >= periodStartMonth && new DateTime(s.Year, s.Month, 1) <= periodEndMonth)
                .Sum(s => s.NetAmount);

            decimal otherProjectExpenses = await _context.ProjectExpenses
                .Where(e => e.ProjectId.HasValue && projectIds.Contains(e.ProjectId.Value))
                .SumAsync(e => e.Amount);

            var attendanceRecords = await _context.AttendanceDailyRecords
                .Where(r => r.UserId == engineerId && r.Date >= periodStart.Date && r.Date <= periodEnd.Date)
                .ToListAsync();
            int presentDays = attendanceRecords.Count(r => r.CheckIn != null);
            int expectedDays = 0;
            for (var d = periodStart.Date; d <= periodEnd.Date; d = d.AddDays(1))
                if (d.DayOfWeek != DayOfWeek.Friday) expectedDays++;
            int absentDays = Math.Max(0, expectedDays - presentDays);
            decimal lateMinutesTotal = attendanceRecords.Sum(r => r.LateMinutes);

            ViewBag.PeriodStart = periodStart;
            ViewBag.PeriodEnd = periodEnd;
            ViewBag.TotalSales = totalSales;
            ViewBag.TotalSalaryInPeriod = totalSalaryInPeriod;
            ViewBag.OtherProjectExpenses = otherProjectExpenses;
            ViewBag.NetProfit = totalSales - totalSalaryInPeriod - otherProjectExpenses;
            ViewBag.PresentDays = presentDays;
            ViewBag.AbsentDays = absentDays;
            ViewBag.LateMinutesTotal = lateMinutesTotal;

            return View();
        }

        [RequirePermission("Finance.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disburse(string engineerId, List<int> projectIds, decimal amount, string? notes)
        {
            var engineer = await _context.Users.FindAsync(engineerId);
            if (engineer == null)
                return NotFound();

            var category = await _context.ExpenseCategories.FirstOrDefaultAsync(c => c.NameAr == "مكافآت" && c.Scope == ExpenseCategoryScope.General);
            if (category == null)
            {
                TempData["Error"] = "تصنيف المكافآت غير موجود — تواصلي مع الدعم الفني";
                return RedirectToAction("Index", new { engineerId, projectIds });
            }

            var projects = await _context.Projects.Where(p => projectIds.Contains(p.Id)).ToListAsync();
            var projectsLabel = string.Join("، ", projects.Select(p => $"{p.Code} - {p.Name}"));

            var expense = new ProjectExpense
            {
                ProjectId = null,
                ExpenseCategoryId = category.Id,
                Amount = amount,
                Date = DateTime.UtcNow,
                Description = $"مكافأة أداء للمهندسة {engineer.FullName} عن المشاريع: {projectsLabel}" + (string.IsNullOrWhiteSpace(notes) ? "" : $" — {notes}")
            };
            _context.ProjectExpenses.Add(expense);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Create", nameof(ProjectExpense), expense.Id.ToString(), $"صرف مكافأة أداء لـ{engineer.FullName}");
            await _notify.NotifyAsync(engineerId, $"تم صرف مكافأة أداء لكِ بقيمة {amount:N0} د.ل", NotificationEventType.EngineerRewardPaid, null, entityType: "ProjectExpense", entityId: expense.Id);

            TempData["Success"] = $"تم صرف مكافأة أداء بقيمة {amount:N0} د.ل لـ{engineer.FullName}";
            return RedirectToAction("Index", new { engineerId, projectIds });
        }
    }
}