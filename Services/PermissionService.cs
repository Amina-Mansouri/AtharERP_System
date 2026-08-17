using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Services
{
    public class PermissionService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PermissionService(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // الصلاحية الفعلية = صلاحيات الدور UNION الصلاحيات الممنوحة يدوياً للموظف (قاعدة العمل رقم 5)
        public async Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string permissionCode)
        {
            if (principal.Identity == null || !principal.Identity.IsAuthenticated)
                return false;

            var user = await _userManager.GetUserAsync(principal);
            if (user == null || !user.IsActive)
                return false;

            var permissionId = await _context.Permissions
                .Where(p => p.Code == permissionCode && p.IsActive)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (permissionId == null)
                return false;

            // 1) صلاحية إضافية ممنوحة يدوياً لهذا الموظف بعينه (اتحاد إضافي فقط)
            var hasOverride = await _context.UserPermissions
                .AnyAsync(up => up.UserId == user.Id && up.PermissionId == permissionId.Value);

            if (hasOverride)
                return true;

            // 2) صلاحية عبر دور/أدوار المستخدم
            var roleNames = (await _userManager.GetRolesAsync(user)).ToList();
            if (roleNames.Count == 0)
                return false;

            var roleIds = await _context.Roles
                .Where(r => roleNames.Contains(r.Name) && r.IsActive)
                .Select(r => r.Id)
                .ToListAsync();

            if (roleIds.Count == 0)
                return false;

            return await _context.RolePermissions
                .AnyAsync(rp => roleIds.Contains(rp.RoleId) && rp.PermissionId == permissionId.Value && rp.IsGranted);
        }
    }
}