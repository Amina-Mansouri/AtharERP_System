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

        // الصلاحية الفعلية = صلاحيات دور/أدوار المستخدم فقط (لا يوجد منح يدوي فردي بعد إلغاء UserPermission)
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

        public class EffectivePermission
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Module { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty; // دائماً "دور" بعد إلغاء المنح اليدوي
        }

        // البند 8 في BACKEND.md: صلاحيات دور المستخدم (بعد إلغاء الاستثناءات اليدوية، الإضافة تتم عبر إنشاء دور جديد له)
        public async Task<List<EffectivePermission>> GetEffectivePermissionsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new List<EffectivePermission>();

            var roleNames = (await _userManager.GetRolesAsync(user)).ToList();
            var roleIds = await _context.Roles
                .Where(r => roleNames.Contains(r.Name) && r.IsActive)
                .Select(r => r.Id)
                .ToListAsync();

            var rolePermissionIds = roleIds.Count == 0
                ? new List<int>()
                : await _context.RolePermissions
                    .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsGranted)
                    .Select(rp => rp.PermissionId)
                    .Distinct()
                    .ToListAsync();

            var permissions = await _context.Permissions
                .Where(p => rolePermissionIds.Contains(p.Id) && p.IsActive)
                .OrderBy(p => p.Module).ThenBy(p => p.Code)
                .ToListAsync();

            return permissions.Select(p => new EffectivePermission
            {
                Code = p.Code,
                Name = p.Name,
                Module = p.Module,
                Source = "دور"
            }).ToList();
        }
        // وصول المستخدم لمشروع معيّن: منشئه، أو عضو في فريقه، أو لديه Projects.ViewAll
        // (يُستخدم من كنترولرات المواقع لعزل الرؤية حسب فريق المشروع)
        public async Task<bool> CanAccessProjectAsync(ClaimsPrincipal principal, int projectId)
        {
            if (await HasPermissionAsync(principal, "Projects.ViewAll"))
                return true;

            var user = await _userManager.GetUserAsync(principal);
            if (user == null)
                return false;

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                return false;

            if (project.CreatedById == user.Id)
                return true;

            return await _context.ProjectTeamMembers.AnyAsync(tm => tm.ProjectId == projectId && tm.UserId == user.Id);
        }
    }
}