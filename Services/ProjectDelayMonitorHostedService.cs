using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    // مراقبة يومية لكل المشاريع النشطة — تحدّث حالة "متأخر" تلقائياً حسب قرب التسليم ونسبة الإنجاز
    public class ProjectDelayMonitorHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ProjectDelayMonitorHostedService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task ProcessAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var calc = scope.ServiceProvider.GetRequiredService<ProjectCalculationService>();

            var projectIds = await context.Projects
                .Where(p => p.Status == ProjectStatus.InProgress || p.Status == ProjectStatus.Delayed)
                .Select(p => p.Id)
                .ToListAsync(stoppingToken);

            foreach (var id in projectIds)
            {
                await calc.RecalculateProjectAsync(id);
            }
        }
    }
}