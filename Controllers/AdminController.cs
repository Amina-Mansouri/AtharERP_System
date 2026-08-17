using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
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
            var users = await _userManager.Users
                .Include(u => u.Department)
                .OrderBy(u => u.FullName)
                .ToListAsync();

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
            ViewBag.CurrentRole = currentRoles.FirstOrDefault();
            await ReloadEditUserViewBagsAsync(id, ViewBag.CurrentRole);

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(
            string id,
            [Bind("FullName,JobNumber,PersonalId,Responsibilities,ProfilePhotoPath,DocumentsPath,DepartmentId,Rank,CareerTrack,ContractSalary,ContractStartDate,ContractEndDate,MonthlyEvaluationDate,YearlyEvaluationDate,ContractTerminationDate,Pledge,PhoneNumber,ExpectedLocationName,ExpectedLatitude,ExpectedLongitude,AllowedRadiusMeters")] ApplicationUser model,
            string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // قاعدة العمل 3: كل موظف يجب أن يكون مرتبطاً بقسم
            if (model.DepartmentId == null)
            {
                ModelState.AddModelError(string.Empty, "القسم مطلوب");
            }

            if (!ModelState.IsValid)
            {
                await ReloadEditUserViewBagsAsync(id, role);
                return View(user);
            }

            user.FullName = model.FullName;
            user.JobNumber = model.JobNumber;
            user.PersonalId = model.PersonalId;
            user.Responsibilities = model.Responsibilities;
            user.ProfilePhotoPath = model.ProfilePhotoPath;
            user.DocumentsPath = model.DocumentsPath;
            user.DepartmentId = model.DepartmentId;
            user.Rank = model.Rank;
            user.CareerTrack = model.CareerTrack;
            user.ContractSalary = model.ContractSalary;
            user.ContractStartDate = model.ContractStartDate;
            user.ContractEndDate = model.ContractEndDate;
            user.MonthlyEvaluationDate = model.MonthlyEvaluationDate;
            user.YearlyEvaluationDate = model.YearlyEvaluationDate;
            user.ContractTerminationDate = model.ContractTerminationDate;
            user.Pledge = model.Pledge;
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

                await ReloadEditUserViewBagsAsync(id, role);
                return View(user);
            }

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

            // قاعدة العمل 2: لا يمكن تعطيل آخر مدير نظام نشط في النظام
            if (user.IsActive && await _userManager.IsInRoleAsync(user, "مدير النظام"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("مدير النظام");
                var otherActiveAdmins = admins.Count(u => u.Id != user.Id && u.IsActive);
                if (otherActiveAdmins == 0)
                {
                    TempData["Error"] = "لا يمكن تعطيل آخر مدير نظام نشط في النظام";
                    return RedirectToAction("Users");
                }
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            if (!user.IsActive)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }

            TempData["Success"] = user.IsActive ? $"تم تفعيل {user.FullName}" : $"تم تعطيل {user.FullName}";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? $"تم تحديث كلمة مرور {user.FullName} بنجاح"
                : string.Join("، ", result.Errors.Select(e => e.Description));

            return RedirectToAction("EditUser", new { id });
        }

        // ============================================
        // المناصب المتعددة (EmployeePosition)
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPosition(
            string userId,
            [Bind("DepartmentId,Rank,Track,StartDate,IsPrimary")] EmployeePosition model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات المنصب غير صحيحة";
                return RedirectToAction("EditUser", new { id = userId });
            }

            model.UserId = userId;

            if (model.IsPrimary)
            {
                var existingPrimary = await _context.EmployeePositions
                    .Where(ep => ep.UserId == userId && ep.IsPrimary && ep.EndDate == null)
                    .ToListAsync();
                foreach (var p in existingPrimary)
                    p.IsPrimary = false;

                user.DepartmentId = model.DepartmentId;
                user.Rank = model.Rank;
                user.CareerTrack = model.Track;
                await _userManager.UpdateAsync(user);
            }

            _context.EmployeePositions.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة المنصب بنجاح";
            return RedirectToAction("EditUser", new { id = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndPosition(int id, string userId)
        {
            var position = await _context.EmployeePositions.FindAsync(id);
            if (position != null)
            {
                position.EndDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "تم إنهاء المنصب بنجاح";
            return RedirectToAction("EditUser", new { id = userId });
        }

        // ============================================
        // الصلاحيات الإضافية الممنوحة يدوياً (UserPermission)
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserPermission(string userId, int permissionId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var exists = await _context.UserPermissions.AnyAsync(up => up.UserId == userId && up.PermissionId == permissionId);
            if (!exists)
            {
                _context.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = permissionId, GrantedAt = DateTime.UtcNow });
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم منح الصلاحية الإضافية بنجاح";
            }

            return RedirectToAction("EditUser", new { id = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserPermission(int id, string userId)
        {
            var link = await _context.UserPermissions.FindAsync(id);
            if (link != null)
            {
                _context.UserPermissions.Remove(link);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "تمت إزالة الصلاحية الإضافية";
            return RedirectToAction("EditUser", new { id = userId });
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
                .OrderBy(p => p.Module).ThenBy(p => p.Code)
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
                    .OrderBy(p => p.Module).ThenBy(p => p.Code)
                    .ToListAsync();
                ViewBag.SelectedPermissionIds = selectedPermissions?.ToList() ?? new List<int>();
                return View(model);
            }

            model.CreatedAt = DateTime.UtcNow;
            model.CanDelete = true;
            var result = await _roleManager.CreateAsync(model);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                ViewBag.AllPermissions = await _context.Permissions
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module).ThenBy(p => p.Code)
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
                        PermissionId = permId,
                        IsGranted = true
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
                .OrderBy(p => p.Module).ThenBy(p => p.Code)
                .ToListAsync();

            ViewBag.SelectedPermissionIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == id && rp.IsGranted)
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
                    .OrderBy(p => p.Module).ThenBy(p => p.Code)
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
                    .OrderBy(p => p.Module).ThenBy(p => p.Code)
                    .ToListAsync();
                ViewBag.SelectedPermissionIds = selectedPermissions?.ToList() ?? new List<int>();
                return View(role);
            }

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
                        PermissionId = permId,
                        IsGranted = true
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

            // قاعدة العمل 1: الأدوار القوالب محمية من الحذف (CanDelete = false)
            if (!role.CanDelete)
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

        // ============================================
        // إدارة الأقسام (Department)
        // ============================================

        [HttpGet]
        public async Task<IActionResult> Departments()
        {
            var departments = await _context.Departments
                .Include(d => d.ParentDepartment)
                .Include(d => d.Users)
                .OrderBy(d => d.ParentDepartmentId == null ? 0 : 1)
                .ThenBy(d => d.Name)
                .ToListAsync();

            return View(departments);
        }

        [HttpGet]
        public async Task<IActionResult> CreateDepartment()
        {
            ViewBag.ParentDepartments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment([Bind("Name,ParentDepartmentId,Description")] Department model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ParentDepartments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
                return View(model);
            }

            model.IsActive = true;
            model.CreatedAt = DateTime.UtcNow;

            _context.Departments.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم إنشاء القسم {model.Name} بنجاح";
            return RedirectToAction("Departments");
        }

        [HttpGet]
        public async Task<IActionResult> EditDepartment(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            ViewBag.ParentDepartments = await _context.Departments
                .Where(d => d.IsActive && d.Id != id)
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDepartment(int id, [Bind("Name,ParentDepartmentId,Description,IsActive")] Department model)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            if (model.ParentDepartmentId == id)
            {
                ModelState.AddModelError(string.Empty, "لا يمكن أن يكون القسم أباً لنفسه");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ParentDepartments = await _context.Departments
                    .Where(d => d.IsActive && d.Id != id)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
                return View(model);
            }

            department.Name = model.Name;
            department.ParentDepartmentId = model.ParentDepartmentId;
            department.Description = model.Description;
            department.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم تحديث القسم {department.Name} بنجاح";
            return RedirectToAction("Departments");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var department = await _context.Departments
                .Include(d => d.ChildDepartments)
                .Include(d => d.Users)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
                return NotFound();

            if (department.ChildDepartments.Any())
            {
                TempData["Error"] = "لا يمكن حذف القسم لوجود أقسام فرعية مرتبطة به";
                return RedirectToAction("Departments");
            }

            if (department.Users.Any())
            {
                TempData["Error"] = "لا يمكن حذف القسم لوجود موظفين مرتبطين به";
                return RedirectToAction("Departments");
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف القسم بنجاح";
            return RedirectToAction("Departments");
        }

        // ============================================
        // دالة مساعدة
        // ============================================
        private async Task ReloadEditUserViewBagsAsync(string userId, string? selectedRole)
        {
            ViewBag.Roles = await _roleManager.Roles.Where(r => r.IsActive).Select(r => r.Name).ToListAsync();
            ViewBag.CurrentRole = selectedRole;
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.JobRanks = EnumDisplayHelper.GetDisplayList<JobRank>();
            ViewBag.CareerTracks = EnumDisplayHelper.GetDisplayList<CareerTrack>();

            ViewBag.Positions = await _context.EmployeePositions
                .Include(ep => ep.Department)
                .Where(ep => ep.UserId == userId)
                .OrderByDescending(ep => ep.StartDate)
                .ToListAsync();

            ViewBag.AllPermissions = await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Module).ThenBy(p => p.Code)
                .ToListAsync();

            ViewBag.UserOverridePermissions = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == userId)
                .ToListAsync();
        }
    }
}