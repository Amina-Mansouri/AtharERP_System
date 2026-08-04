using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    // فقط مدير النظام يمكنه الوصول لأي إجراء داخل هذا المتحكم
    [Authorize(Roles = "مدير النظام")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AppDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // ============================================
        // إدارة المستخدمين
        // ============================================

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();

            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);

            ViewBag.Roles = await _roleManager.Roles.Where(r => r.IsActive).Select(r => r.Name).ToListAsync();
            ViewBag.CurrentRole = currentRoles.FirstOrDefault();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(
            string id,
            [Bind("FullName,JobTitle,Department,EngineerRank,PhoneNumber,ExpectedLocationName,ExpectedLatitude,ExpectedLongitude,AllowedRadiusMeters")] ApplicationUser model,
            string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _roleManager.Roles.Where(r => r.IsActive).Select(r => r.Name).ToListAsync();
                ViewBag.CurrentRole = role;
                return View(user);
            }

            user.FullName = model.FullName;
            user.JobTitle = model.JobTitle;
            user.Department = model.Department;
            user.EngineerRank = model.EngineerRank;
            user.PhoneNumber = model.PhoneNumber;
            user.ExpectedLocationName = model.ExpectedLocationName;
            user.ExpectedLatitude = model.ExpectedLatitude;
            user.ExpectedLongitude = model.ExpectedLongitude;
            user.AllowedRadiusMeters = model.AllowedRadiusMeters;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                ViewBag.Roles = await _roleManager.Roles.Where(r => r.IsActive).Select(r => r.Name).ToListAsync();
                ViewBag.CurrentRole = role;
                return View(user);
            }

            // تحديث الدور (دور واحد لكل مستخدم، بنفس منطق التسجيل في AccountController)
            if (!string.IsNullOrEmpty(role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Count > 0)
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, role);
            }

            TempData["Success"] = $"تم تحديث بيانات {user.FullName} بنجاح";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "لا يمكنك تعطيل حسابك الخاص";
                return RedirectToAction("Users");
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            if (!user.IsActive)
            {
                // إبطال أي جلسة عمل حالية للمستخدم فور تعطيله
                await _userManager.UpdateSecurityStampAsync(user);
            }

            TempData["Success"] = user.IsActive ? $"تم تفعيل {user.FullName}" : $"تم تعطيل {user.FullName}";
            return RedirectToAction("Users");
        }

        // ============================================
        // إدارة الأدوار
        // ============================================

        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

            var permissionCounts = new Dictionary<string, int>();
            var userCounts = new Dictionary<string, int>();

            foreach (var role in roles)
            {
                permissionCounts[role.Id] = await _context.RolePermissions.CountAsync(rp => rp.RoleId == role.Id);
                userCounts[role.Id] = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
            }

            ViewBag.PermissionCounts = permissionCounts;
            ViewBag.UserCounts = userCounts;

            return View(roles);
        }

        [HttpGet]
        public async Task<IActionResult> CreateRole()
        {
            ViewBag.AllPermissions = await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Module).ThenBy(p => p.Action)
                .ToListAsync();
            ViewBag.SelectedPermissionIds = new List<int>();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(
            [Bind("Name,Description,IsActive")] ApplicationRole model,
            int[] selectedPermissions)
        {
            if (!string.IsNullOrEmpty(model.Name) && await _roleManager.RoleExistsAsync(model.Name))
                ModelState.AddModelError(string.Empty, "اسم الدور مستخدم بالفعل");

            if (!ModelState.IsValid)
            {
                ViewBag.AllPermissions = await _context.Permissions
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module).ThenBy(p => p.Action)
                    .ToListAsync();
                ViewBag.SelectedPermissionIds = selectedPermissions?.ToList() ?? new List<int>();
                return View(model);
            }

            model.CreatedAt = DateTime.UtcNow;
            var result = await _roleManager.CreateAsync(model);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                ViewBag.AllPermissions = await _context.Permissions
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module).ThenBy(p => p.Action)
                    .ToListAsync();
                ViewBag.SelectedPermissionIds = selectedPermissions?.ToList() ?? new List<int>();
                return View(model);
            }

            if (selectedPermissions != null && selectedPermissions.Length > 0)
            {
                foreach (var permId in selectedPermissions)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = model.Id,
                        PermissionId = permId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"تم إنشاء الدور {model.Name} بنجاح";
            return RedirectToAction("Roles");
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            ViewBag.AllPermissions = await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Module).ThenBy(p => p.Action)
                .ToListAsync();

            ViewBag.SelectedPermissionIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(
            string id,
            [Bind("Name,Description,IsActive")] ApplicationRole model,
            int[] selectedPermissions)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            if (role.IsTemplate && !string.Equals(model.Name, role.Name, StringComparison.Ordinal))
                ModelState.AddModelError(string.Empty, "لا يمكن تغيير اسم دور قالب جاهز");

            if (!ModelState.IsValid)
            {
                ViewBag.AllPermissions = await _context.Permissions
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module).ThenBy(p => p.Action)
                    .ToListAsync();
                ViewBag.SelectedPermissionIds = selectedPermissions?.ToList() ?? new List<int>();
                return View(role);
            }

            role.Name = model.Name;
            role.Description = model.Description;
            role.IsActive = model.IsActive;

            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                ViewBag.AllPermissions = await _context.Permissions
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module).ThenBy(p => p.Action)
                    .ToListAsync();
                ViewBag.SelectedPermissionIds = selectedPermissions?.ToList() ?? new List<int>();
                return View(role);
            }

            // إعادة ضبط الصلاحيات: حذف الحالية ثم إضافة المختارة (على دفعتين لتفادي تعارض الفهرس الفريد)
            var existingLinks = await _context.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
            _context.RolePermissions.RemoveRange(existingLinks);
            await _context.SaveChangesAsync();

            if (selectedPermissions != null && selectedPermissions.Length > 0)
            {
                foreach (var permId in selectedPermissions)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = id,
                        PermissionId = permId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"تم تحديث الدور {role.Name} بنجاح";
            return RedirectToAction("Roles");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            if (role.IsTemplate)
            {
                TempData["Error"] = "لا يمكن حذف دور قالب جاهز";
                return RedirectToAction("Roles");
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Count > 0)
            {
                TempData["Error"] = $"لا يمكن حذف الدور، يوجد {usersInRole.Count} مستخدم مرتبط به";
                return RedirectToAction("Roles");
            }

            await _roleManager.DeleteAsync(role);

            TempData["Success"] = $"تم حذف الدور {role.Name} بنجاح";
            return RedirectToAction("Roles");
        }
    }
}