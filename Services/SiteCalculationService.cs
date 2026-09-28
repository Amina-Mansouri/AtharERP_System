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

        public async Task<decimal> CalculateSiteCostAsync(int siteId, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var maintenanceQuery = _context.SiteMaintenances.Where(m => m.SiteId == siteId);
            if (dateFrom.HasValue) maintenanceQuery = maintenanceQuery.Where(m => m.RequestDate >= dateFrom.Value);
            if (dateTo.HasValue) maintenanceQuery = maintenanceQuery.Where(m => m.RequestDate <= dateTo.Value.AddDays(1).AddTicks(-1));
            var maintenanceCost = await maintenanceQuery.SumAsync(m => m.Cost ?? 0);

            var contractorQuery = _context.SiteContractors.Where(c => c.SiteId == siteId);
            if (dateFrom.HasValue) contractorQuery = contractorQuery.Where(c => !c.StartDate.HasValue || c.StartDate.Value <= dateTo);
            if (dateTo.HasValue) contractorQuery = contractorQuery.Where(c => !c.EndDate.HasValue || c.EndDate.Value >= dateFrom);
            var contractorCost = await contractorQuery.SumAsync(c => c.Amount ?? 0);

            var supplyQuery = _context.SiteSupplyRequests.Where(s => s.SiteId == siteId);
            if (dateFrom.HasValue) supplyQuery = supplyQuery.Where(s => s.RequestDate >= dateFrom.Value);
            if (dateTo.HasValue) supplyQuery = supplyQuery.Where(s => s.RequestDate <= dateTo.Value.AddDays(1).AddTicks(-1));
            var supplyCost = await supplyQuery.SumAsync(s => s.Quantity * (s.UnitPrice ?? 0));

            return maintenanceCost + contractorCost + supplyCost;
        }

        public async Task<decimal> CalculateProjectSiteCostsAsync(int projectId, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var siteIds = await _context.Sites.Where(s => s.ProjectId == projectId).Select(s => s.Id).ToListAsync();
            decimal structuredCost = 0;
            foreach (var siteId in siteIds)
            {
                structuredCost += await CalculateSiteCostAsync(siteId, dateFrom, dateTo);
            }

            var expensesQuery = _context.ProjectExpenses.Where(e => siteIds.Contains(e.SiteId ?? 0));
            if (dateFrom.HasValue) expensesQuery = expensesQuery.Where(e => e.Date >= dateFrom.Value);
            if (dateTo.HasValue) expensesQuery = expensesQuery.Where(e => e.Date <= dateTo.Value);
            var manualSiteExpenses = await expensesQuery.SumAsync(e => e.Amount);

            return structuredCost + manualSiteExpenses;
        }
    }
}