using AtharERP_System.Data;
using AtharERP_System.Models.Entities;

namespace AtharERP_System.Services
{
    // سجل تدقيق بسيط لكل عمليات الإضافة/التعديل/الحذف
    public class AuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userId, string action, string entityName, string entityId, string? details = null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}