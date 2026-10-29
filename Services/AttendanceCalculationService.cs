using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    // يحسب ملخص الحضور اليومي (دخول/خروج/تأخير/انصراف مبكر/إضافي) من البصمات الخام
    // وقت الدوام الرسمي موحَّد حاليًا لكل الموظفين: 9:00 صباحًا – 4:00 عصرًا (7 ساعات)
    public class AttendanceCalculationService
    {
        private static readonly TimeSpan OfficeUtcOffset = TimeSpan.FromHours(2); // توقيت ليبيا — ثابت، بلا توقيت صيفي
        private static readonly TimeSpan OfficialStart = new TimeSpan(9, 0, 0);
        private static readonly TimeSpan OfficialEnd = new TimeSpan(16, 0, 0);
        private const decimal OfficialWorkHours = 7m;

        private readonly AppDbContext _context;

        public AttendanceCalculationService(AppDbContext context)
        {
            _context = context;
        }

        // يُستدعى بعد استيراد دفعة بصمات جديدة — يعيد حساب أيام (موظف، تاريخ) المتأثرة فقط
        public async Task RecalculateDaysAsync(IEnumerable<(string UserId, DateTime Date)> affectedDays)
        {
            var distinctDays = affectedDays.Distinct().ToList();

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

                decimal? totalHours = null;
                decimal lateMinutes = 0;
                decimal earlyLeaveMinutes = 0;
                decimal overtimeHours = 0;

                if (checkOut > checkIn)
                {
                    totalHours = (decimal)(checkOut - checkIn).TotalHours;

                    if (totalHours > OfficialWorkHours)
                        overtimeHours = Math.Round(totalHours.Value - OfficialWorkHours, 2);
                }

                var officialStartToday = dayStart.Add(OfficialStart).Subtract(OfficeUtcOffset);
                var officialEndToday = dayStart.Add(OfficialEnd).Subtract(OfficeUtcOffset);

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