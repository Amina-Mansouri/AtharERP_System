using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    // نقطة استقبال البصمات من الوكيل الجسر داخل شبكة المكتب — محمية بمفتاح سري ثابت، ليست لمستخدمي النظام
    [Route("api/attendance")]
    public class AttendanceSyncController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AttendanceCalculationService _calc;
        private readonly IConfiguration _config;

        public AttendanceSyncController(AppDbContext context, AttendanceCalculationService calc, IConfiguration config)
        {
            _context = context;
            _calc = calc;
            _config = config;
        }

        public class PunchDto
        {
            public string JobNumber { get; set; } = string.Empty;
            public DateTime PunchTime { get; set; }
            public string SourceTransactionId { get; set; } = string.Empty;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromBody] List<PunchDto> punches)
        {
            var expectedKey = _config["Attendance:SyncApiKey"];
            if (string.IsNullOrEmpty(expectedKey) || Request.Headers["X-Api-Key"] != expectedKey)
                return Unauthorized();

            if (punches == null || !punches.Any())
                return Ok(new { imported = 0 });

            var jobNumbers = punches.Select(p => p.JobNumber).Distinct().ToList();
            var users = await _context.Users
                .Where(u => u.JobNumber != null && jobNumbers.Contains(u.JobNumber))
                .ToDictionaryAsync(u => u.JobNumber!, u => u.Id);

            var existingTxIds = await _context.AttendancePunches
                .Where(p => punches.Select(x => x.SourceTransactionId).Contains(p.SourceTransactionId))
                .Select(p => p.SourceTransactionId)
                .ToListAsync();

            var affectedDays = new List<(string UserId, DateTime Date)>();
            var imported = 0;
            var unmatched = new List<string>();

            foreach (var p in punches)
            {
                if (existingTxIds.Contains(p.SourceTransactionId))
                    continue;

                if (!users.TryGetValue(p.JobNumber, out var userId))
                {
                    unmatched.Add(p.JobNumber);
                    continue;
                }

                _context.AttendancePunches.Add(new AttendancePunch
                {
                    UserId = userId,
                    PunchTime = p.PunchTime,
                    SourceTransactionId = p.SourceTransactionId
                });

                affectedDays.Add((userId, p.PunchTime.AddHours(2).Date));
                imported++;
            }

            await _context.SaveChangesAsync();

            if (affectedDays.Any())
                await _calc.RecalculateDaysAsync(affectedDays);

            return Ok(new { imported, unmatchedJobNumbers = unmatched.Distinct() });
        }
    }
}