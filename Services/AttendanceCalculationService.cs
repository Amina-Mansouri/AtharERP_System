using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    // يحسب ملخص الحضور اليومي (دخول/خروج/تأخير/انصراف مبكر/إضافي) من البصمات الخام
    // وقت الدوام الرسمي أصبح خاصاً بكل موظف (ExpectedStartTime/ExpectedEndTime على بياناته) —
    // وإن لم يُحدَّد له وقت مخصَّص بعد، يُستخدم الافتراضي القديم (9 صباحًا - 4 عصرًا) كاحتياط
    public class AttendanceCalculationService
    {
        private static readonly TimeSpan OfficeUtcOffset = TimeSpan.FromHours(2); // توقيت ليبيا — ثابت، بلا توقيت صيفي
        private static readonly TimeSpan DefaultOfficialStart = new TimeSpan(9, 0, 0);
        private static readonly TimeSpan DefaultOfficialEnd = new TimeSpan(16, 0, 0);

        private readonly AppDbContext _context;

        public AttendanceCalculationService(AppDbContext context)
        {
            _context = context;
        }

        // يُستدعى بعد استيراد دفعة بصمات جديدة — يعيد حساب أيام (موظف، تاريخ) المتأثرة فقط
        public async Task RecalculateDaysAsync(IEnumerable<(string UserId, DateTime Date)> affectedDays)
        {
            var distinctDays = affectedDays.Distinct().ToList();
            var userIds = distinctDays.Select(d => d.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            foreach (var (userId, date) in distinctDays)
            {
                var dayStart = date.Date;
                var dayEnd = dayStart.AddDays(1);

                var punches = await _context.AttendancePunches
                    .Where(p => p.UserId == userId && p.PunchTime >= dayStart && p.PunchTime < dayEnd)
                    .OrderBy(p => p.PunchTime)
                    .ToListAsync();

                if (!punches.Any())
                    continue;

                var checkIn = punches.First().PunchTime;
                var checkOut = punches.Last().PunchTime;

                var user = users.TryGetValue(userId, out var u) ? u : null;
                var officialStart = user?.ExpectedStartTime?.ToTimeSpan() ?? DefaultOfficialStart;
                var officialEnd = user?.ExpectedEndTime?.ToTimeSpan() ?? DefaultOfficialEnd;
                var officialWorkHours = (decimal)(officialEnd - officialStart).TotalHours;
                if (officialWorkHours <= 0)
                {
                    officialStart = DefaultOfficialStart;
                    officialEnd = DefaultOfficialEnd;
                    officialWorkHours = (decimal)(DefaultOfficialEnd - DefaultOfficialStart).TotalHours;
                }

                decimal? totalHours = null;
                decimal lateMinutes = 0;
                decimal earlyLeaveMinutes = 0;
                decimal overtimeHours = 0;

                if (checkOut > checkIn)
                {
                    totalHours = (decimal)(checkOut - checkIn).TotalHours;

                    if (totalHours > officialWorkHours)
                        overtimeHours = Math.Round(totalHours.Value - officialWorkHours, 2);
                }

                var officialStartToday = dayStart.Add(officialStart).Subtract(OfficeUtcOffset);
                var officialEndToday = dayStart.Add(officialEnd).Subtract(OfficeUtcOffset);

                if (checkIn > officialStartToday)
                    lateMinutes = Math.Round((decimal)(checkIn - officialStartToday).TotalMinutes, 2);

                if (checkOut < officialEndToday && checkOut > checkIn)
                    earlyLeaveMinutes = Math.Round((decimal)(officialEndToday - checkOut).TotalMinutes, 2);

                var record = await _context.AttendanceDailyRecords
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.Date == dayStart);

                if (record == null)
                {
                    record = new AttendanceDailyRecord { UserId = userId, Date = dayStart };
                    _context.AttendanceDailyRecords.Add(record);
                }

                record.CheckIn = checkIn;
                record.CheckOut = checkOut == checkIn ? null : checkOut;
                record.TotalHours = totalHours;
                record.LateMinutes = lateMinutes;
                record.EarlyLeaveMinutes = earlyLeaveMinutes;

                // إن تغيّرت ساعات الإضافي عن القيمة المعتمدة سابقًا، يعود القرار "بانتظار الاعتماد" من جديد
                if (record.OvertimeHours != overtimeHours)
                {
                    record.OvertimeHours = overtimeHours;
                    if (overtimeHours <= 0)
                    {
                        record.OvertimeApproved = null;
                        record.OvertimeDecidedById = null;
                        record.OvertimeDecidedAt = null;
                    }
                    else
                    {
                        record.OvertimeApproved = null;
                        record.OvertimeDecidedById = null;
                        record.OvertimeDecidedAt = null;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}