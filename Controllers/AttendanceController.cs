using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public AttendanceController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("HR.Attendance.View")]
        public async Task<IActionResult> Index(int? departmentId, string? userId, DateTime? dateFrom, DateTime? dateTo)
        {
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.DepartmentId = departmentId;
            ViewBag.UserId = userId;

            var usersQuery = _context.Users.Where(u => u.IsActive);
            if (departmentId.HasValue)
                usersQuery = usersQuery.Where(u => u.DepartmentId == departmentId.Value);
            ViewBag.Users = await usersQuery.OrderBy(u => u.FirstName).ToListAsync();

            dateFrom ??= DateTime.UtcNow.Date.AddDays(-DateTime.UtcNow.Day + 1);
            dateTo ??= DateTime.UtcNow.Date;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            var query = _context.AttendanceDailyRecords
                .Include(r => r.User)
                .Include(r => r.OvertimeDecidedBy)
                .Where(r => r.Date >= dateFrom.Value && r.Date <= dateTo.Value);

            if (departmentId.HasValue)
                query = query.Where(r => r.User.DepartmentId == departmentId.Value);
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(r => r.UserId == userId);

            var records = await query
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.User.FirstName)
                .ToListAsync();

            return View(records);
        }

        [RequirePermission("HR.Attendance.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideOvertime(int id, bool approved)
        {
            var record = await _context.AttendanceDailyRecords.FindAsync(id);
            if (record == null)
                return NotFound();

            if (record.OvertimeHours <= 0)
                return BadRequest();

            record.OvertimeApproved = approved;
            record.OvertimeDecidedById = CurrentUserId;
            record.OvertimeDecidedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(AttendanceDailyRecord), record.Id.ToString(), approved ? "اعتماد ساعات إضافية" : "رفض ساعات إضافية");

            TempData["Success"] = approved ? "تم اعتماد الساعات الإضافية" : "تم رفض الساعات الإضافية";
            return RedirectToAction("Index");
        }
    }
}