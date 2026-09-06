using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    // الحالة التلقائية لمراحل العمل والمواقع — مشتركة بين شاشات الموظفين وبوابة المقاول
    public class SiteCalculationService
    {
        private readonly AppDbContext _context;

        public SiteCalculationService(AppDbContext context)
        {
            _context = context;
        }

        public static void ApplyAutomaticOperationStatus(SiteOperation op)
        {
            if (op.ActualEndDate.HasValue)
            {
                op.Status = OperationStatus.Completed;
                op.CompletionPercentage = 100;
                return;
            }

            if (op.PlannedEndDate.HasValue && DateTime.UtcNow.Date > op.PlannedEndDate.Value.Date)
            {
                op.Status = OperationStatus.Delayed;
                return;
            }

            op.Status = op.ActualStartDate.HasValue ? OperationStatus.InProgress : OperationStatus.NotStarted;
        }

        public async Task ApplyAutomaticSiteStatusAsync(int siteId)
        {
            var site = await _context.Sites.Include(s => s.Operations).FirstOrDefaultAsync(s => s.Id == siteId);
            if (site == null || site.Status == SiteStatus.OnHold)
                return;

            if (site.Operations.Any() && site.Operations.All(o => o.Status == OperationStatus.Completed))
            {
                site.Status = SiteStatus.Completed;
                site.ActualEndDate ??= DateTime.UtcNow.Date;
            }
            else
            {
                site.Status = SiteStatus.Active;
            }

            await _context.SaveChangesAsync();
        }
    }
}