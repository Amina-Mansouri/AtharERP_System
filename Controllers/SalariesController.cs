using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SalariesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly FinancePdfService _pdfExport;
        private readonly AuditService _audit;

        public SalariesController(AppDbContext context, PermissionService permissionService, FinancePdfService pdfExport, AuditService audit)
        {
            _context = context;
            _permissionService = permissionService;
            _pdfExport = pdfExport;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("HR.Salaries.View")]
        public async Task<IActionResult> Index(int? year, int? month)
        {
            var now = DateTime.UtcNow;
            int y = year ?? now.Year;
            int m = month ?? now.Month;
            bool isCurrentMonth = y == now.Year && m == now.Month;

            var employees = await _context.Users
                .Include(u => u.JobRankRef)
                .Include(u => u.PositionRef)
                .Where(u => u.IsActive)
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            var existingPayments = await _context.SalaryPayments
                .Where(s => s.Year == y && s.Month == m)
                .ToDictionaryAsync(s => s.UserId);

            var monthStart = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);
            var today = DateTime.UtcNow.Date;

            var userIds = employees.Select(e => e.Id).ToList();
            var attendanceRecords = await _context.AttendanceDailyRecords
                .Where(r => userIds.Contains(r.UserId) && r.Date >= monthStart && r.Date < monthEnd)
                .ToListAsync();

            var previews = new List<object>();
            foreach (var emp in employees)
            {
                if (existingPayments.ContainsKey(emp.Id))
                    continue;

                var empRecords = attendanceRecords.Where(r => r.UserId == emp.Id).ToList();
                decimal workedHours = empRecords.Sum(r => r.TotalHours ?? 0);
                decimal overtimeHoursApproved = empRecords.Where(r => r.OvertimeApproved == true).Sum(r => r.OvertimeHours);
                decimal lateMinutesTotal = empRecords.Sum(r => r.LateMinutes + r.EarlyLeaveMinutes);

                var periodEnd = monthEnd <= today.AddDays(1) ? monthEnd : today.AddDays(1);
                int expectedWorkDays = 0;
                for (var d = monthStart; d < periodEnd; d = d.AddDays(1))
                {
                    if (d.DayOfWeek != DayOfWeek.Friday) expectedWorkDays++;
                }
                var daysPresent = empRecords.Where(r => r.CheckIn != null).Select(r => r.Date.Date).Distinct().Count();
                int estimatedAbsentDays = Math.Max(0, expectedWorkDays - daysPresent);

                decimal? hourlyRateUsed = emp.JobRankRef?.HourlyRate;
                decimal? fixedSalaryUsed = emp.JobRankRef?.BaseSalary;
                decimal positionPercentage = emp.PositionRef?.Percentage ?? 0;
                decimal overtimePercentage = emp.JobRankRef?.OvertimePercentage ?? 0;

                decimal baseAmount = 0;
                decimal overtimeAmount = 0;

                if (isCurrentMonth)
                {
                    if (hourlyRateUsed.HasValue && hourlyRateUsed.Value > 0)
                    {
                        var positionBonusPerHour = hourlyRateUsed.Value * (positionPercentage / 100m);
                        var effectiveHourlyRate = hourlyRateUsed.Value + positionBonusPerHour;
                        baseAmount = Math.Round(effectiveHourlyRate * workedHours, 2);
                        var overtimeRate = hourlyRateUsed.Value * (overtimePercentage / 100m);
                        overtimeAmount = Math.Round(overtimeRate * overtimeHoursApproved, 2);
                    }
                    else if (fixedSalaryUsed.HasValue && fixedSalaryUsed.Value > 0)
                    {
                        var positionBonus = fixedSalaryUsed.Value * (positionPercentage / 100m);
                        baseAmount = Math.Round(fixedSalaryUsed.Value + positionBonus, 2);
                        var implicitHourlyRate = fixedSalaryUsed.Value / 240m;
                        var overtimeRate = implicitHourlyRate * (overtimePercentage / 100m);
                        overtimeAmount = Math.Round(overtimeRate * overtimeHoursApproved, 2);
                    }
                }

                previews.Add(new
                {
                    userId = emp.Id,
                    name = emp.FullName,
                    workedHours,
                    overtimeHoursApproved,
                    lateMinutesTotal,
                    estimatedAbsentDays,
                    hourlyRateUsed,
                    fixedSalaryUsed,
                    positionPercentage,
                    overtimePercentage,
                    baseAmount,
                    overtimeAmount
                });
            }

            ViewBag.Employees = employees;
            ViewBag.ExistingPayments = existingPayments;
            ViewBag.Previews = previews;
            ViewBag.Year = y;
            ViewBag.Month = m;
            ViewBag.IsCurrentMonth = isCurrentMonth;
            ViewBag.CanManage = await _permissionService.HasPermissionAsync(User, "HR.Salaries.Edit");

            return View();
        }

        [RequirePermission("HR.Salaries.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(
            string userId, int year, int month,
            decimal workedHours, decimal overtimeHoursApproved, int estimatedAbsentDays, decimal lateMinutesTotal,
            decimal? hourlyRateUsed, decimal? fixedSalaryUsed, decimal positionPercentageUsed, decimal overtimePercentageUsed,
            decimal baseAmount, decimal overtimeAmount, decimal deduction, string? notes)
        {
            var employee = await _context.Users.Include(u => u.JobRankRef).Include(u => u.PositionRef).FirstOrDefaultAsync(u => u.Id == userId);
            if (employee == null)
                return NotFound();

            var exists = await _context.SalaryPayments.AnyAsync(s => s.UserId == userId && s.Year == year && s.Month == month);
            if (exists)
            {
                TempData["Error"] = "تم صرف راتب هذا الموظف لهذا الشهر بالفعل";
                return RedirectToAction("Index", new { year, month });
            }

            var payment = new SalaryPayment
            {
                UserId = userId,
                Year = year,
                Month = month,
                WorkedHours = workedHours,
                OvertimeHoursApproved = overtimeHoursApproved,
                EstimatedAbsentDays = estimatedAbsentDays,
                LateMinutesTotal = lateMinutesTotal,
                HourlyRateUsed = hourlyRateUsed,
                FixedSalaryUsed = fixedSalaryUsed,
                PositionPercentageUsed = positionPercentageUsed,
                OvertimePercentageUsed = overtimePercentageUsed,
                PositionNameUsed = employee.PositionRef?.NameAr,
                RankNameUsed = employee.JobRankRef != null ? $"{employee.JobRankRef.Code} - {employee.JobRankRef.NameAr}" : null,
                BaseAmount = baseAmount,
                OvertimeAmount = overtimeAmount,
                Deduction = deduction,
                NetAmount = baseAmount + overtimeAmount - deduction,
                Notes = notes,
                PaidAt = DateTime.UtcNow,
                PaidById = CurrentUserId
            };
            _context.SalaryPayments.Add(payment);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Create", nameof(SalaryPayment), payment.Id.ToString(), $"صرف راتب {employee.FullName} عن {month}/{year}");

            TempData["Success"] = $"تم صرف راتب {employee.FullName} بنجاح";
            return RedirectToAction("Index", new { year, month });
        }

        [RequirePermission("HR.Salaries.View")]
        public async Task<IActionResult> Receipt(int id)
        {
            var payment = await _context.SalaryPayments
                .Include(s => s.User)
                .Include(s => s.PaidBy)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (payment == null)
                return NotFound();

            var pdf = _pdfExport.GenerateSalaryReceipt(payment);
            return File(pdf, "application/pdf", $"راتب-{payment.User.FullName}-{payment.Month}-{payment.Year}.pdf");
        }

        [RequirePermission("HR.Salaries.View")]
        public async Task<IActionResult> History(string? userId)
        {
            var query = _context.SalaryPayments.Include(s => s.User).Include(s => s.PaidBy).AsQueryable();
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(s => s.UserId == userId);

            var payments = await query.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).ToListAsync();
            ViewBag.UserId = userId;
            return View(payments);
        }
    }
}