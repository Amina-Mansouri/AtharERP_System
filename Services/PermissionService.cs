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

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string permissionName)
        {
            // 1. التحقق من تسجيل الدخول
            if (principal.Identity == null || !principal.Identity.IsAuthenticated)
                return false;

            // 2. جلب المستخدم والتحقق من نشاطه
            var user = await _userManager.GetUserAsync(principal);
            if (user == null || !user.IsActive)
                return false;

            // 3. جلب أسماء الأدوار
            var roleNames = (await _userManager.GetRolesAsync(user)).ToList();
            if (roleNames.Count == 0)
                return false;

            // 4. جلب معرفات الأدوار النشطة
            var roleIds = await _context.Roles
                .Where(r => roleNames.Contains(r.Name) && r.IsActive)
                .Select(r => r.Id)
                .ToListAsync();

            if (roleIds.Count == 0)
                return false;

            // 5. جلب معرف الصلاحية المطلوبة
            var permissionId = await _context.Permissions
                .Where(p => p.Name == permissionName && p.IsActive)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (permissionId == null)
                return false;

            // 6. التحقق من وجود الربط
            return await _context.RolePermissions
                .AnyAsync(rp => roleIds.Contains(rp.RoleId) && rp.PermissionId == permissionId.Value);
        }
    }
}