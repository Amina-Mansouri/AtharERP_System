using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    // وظيفة مجدولة كل ساعة — بند P6/§11: إشعار قبل 30 يوماً من انتهاء العقد، وإيقاف الحساب يوم الانتهاء بلا فقدان بيانات
    public class ContractLifecycleHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ContractLifecycleHostedService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var today = DateTime.UtcNow.Date;
            var soonCutoff = today.AddDays(30);

            var systemAdminIds = await (from ur in context.UserRoles
                                        join r in context.Roles on ur.RoleId equals r.Id
                                        where r.Name == "مدير النظام"
                                        select ur.UserId)
                                        .Distinct()
                                        .ToListAsync(stoppingToken);

            var expiringSoon = await context.Users
                .Where(u => u.IsActive && !u.IsSuspended && u.ContractEndDate != null && u.ContractEndDate.Value.Date == soonCutoff)
                .ToListAsync(stoppingToken);

            foreach (var user in expiringSoon)
            {
                var alreadyNotified = await context.Notifications.AnyAsync(n =>
                    n.EventType == NotificationEventType.ContractExpiring
                    && n.EntityType == "ApplicationUser"
                    && n.EntityId == null
                    && n.Link == $"/Admin/EditUser/{user.Id}"
                    && n.CreatedAt.Date == today, stoppingToken);
                if (alreadyNotified) continue;

                foreach (var recipientId in systemAdminIds.Union(new[] { user.Id }).Distinct())
                {
                    context.Notifications.Add(new Notification
                    {
                        UserId = recipientId,
                        Message = $"عقد الموظف {user.FullName} ينتهي خلال 30 يوماً ({user.ContractEndDate:yyyy-MM-dd})",
                        Link = $"/Admin/EditUser/{user.Id}",
                        EventType = NotificationEventType.ContractExpiring,
                        SourceModule = "01",
                        RequiresAction = true,
                        EntityType = "ApplicationUser",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            var expiredToday = await context.Users
                .Where(u => u.IsActive && !u.IsSuspended && u.ContractEndDate != null && u.ContractEndDate.Value.Date == today)
                .ToListAsync(stoppingToken);

            foreach (var user in expiredToday)
            {
                user.IsSuspended = true;
                user.SuspendedReason = "انتهاء العقد بلا تجديد";

                foreach (var recipientId in systemAdminIds.Union(new[] { user.Id }).Distinct())
                {
                    context.Notifications.Add(new Notification
                    {
                        UserId = recipientId,
                        Message = $"انتهى عقد الموظف {user.FullName} ولم يُجدَّد — تم إيقاف الحساب",
                        Link = $"/Admin/EditUser/{user.Id}",
                        EventType = NotificationEventType.ContractExpired,
                        SourceModule = "01",
                        RequiresAction = true,
                        EntityType = "ApplicationUser",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}