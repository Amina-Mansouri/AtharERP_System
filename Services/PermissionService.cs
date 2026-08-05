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

        // يتحقق ما إذا كان المستخدم الحالي يملك صلاحية معينة عبر أدواره
        public async Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string permissionName)
        {
            if (principal.Identity == null || !principal.Identity.IsAuthenticated)
                return false;

            var user = await _userManager.GetUserAsync(principal);
            if (user == null || !user.IsActive)
                return false;

            var roleNames = await _userManager.GetRolesAsync(user);
            if (roleNames.Count == 0)
                return false;

            return await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .AnyAsync(rp => roleNames.Contains(rp.Role.Name!)
                             && rp.Role.IsActive
                             && rp.Permission.Name == permissionName
                             && rp.Permission.IsActive);
        }
    }
}