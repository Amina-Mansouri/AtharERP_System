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
        private readonly FileUploadService _fileUpload;
        private readonly PermissionService _permissionService;
        private readonly AuditService _auditService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            AppDbContext context,
            FileUploadService fileUpload,
            PermissionService permissionService,
            AuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _fileUpload = fileUpload;
            _permissionService = permissionService;
            _auditService = auditService;
        }

        // ============================================
        // إدارة المستخدمين
        // ============================================

        [HttpGet]
        public async Task<IActionResult> Users(string? q, int? departmentId, JobRank? rank, CareerTrack? track, string? role, string status = "all", int page = 1)
        {
            const int pageSize = 20;

            var query = _userManager.Users
                .Include(u => u.Department)
                .Include(u => u.EmployeePositions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(u => u.FullName.Contains(q) || (u.JobNumber != null && u.JobNumber.Contains(q)));

            if (departmentId.HasValue)
                query = query.Where(u => u.DepartmentId == departmentId);

            if (rank.HasValue)
                query = query.Where(u => u.Rank == rank);

            if (track.HasValue)
                query = query.Where(u => u.CareerTrack == track);

            if (!string.IsNullOrEmpty(role))
            {
                var idsInRole = (await _userManager.GetUsersInRoleAsync(role)).Select(u => u.Id).ToList();
                query = query.Where(u => idsInRole.Contains(u.Id));
            }

            var statusCounts = await query
                .GroupBy(u => u.IsActive)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.CountAll = statusCounts.Sum(c => c.Count);
            ViewBag.CountActive = statusCounts.FirstOrDefault(c => c.Key)?.Count ?? 0;
            ViewBag.CountInactive = statusCounts.FirstOrDefault(c => !c.Key)?.Count ?? 0;

            if (status == "active")
                query = query.Where(u => u.IsActive);
            else if (status == "inactive")
                query = query.Where(u => !u.IsActive);

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);

            ViewBag.UserRoles = userRoles;
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.Roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            ViewBag.CurrentQ = q;
            ViewBag.CurrentDepartmentId = departmentId;
            ViewBag.CurrentRank = rank;
            ViewBag.CurrentTrack = track;
            ViewBag.CurrentRoleFilter = role;
            ViewBag.CurrentStatus = status;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

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
     [Bind("FullName,JobNumber,PersonalId,Responsibilities,DepartmentId,Rank,CareerTrack,ContractSalary,ContractStartDate,ContractEndDate,MonthlyEvaluationDate,YearlyEvaluationDate,ContractTerminationDate,Pledge,PhoneNumber,ExpectedLocationName,ExpectedLatitude,ExpectedLongitude,AllowedRadiusMeters")] ApplicationUser model,
     IFormFile? profilePhoto,
     string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            if (model.DepartmentId == null)
            {
                ModelState.AddModelError(string.Empty, "القسم مطلوب");
            }

            var jobNumber = string.IsNullOrWhiteSpace(model.JobNumber) ? null : model.JobNumber.Trim();
            var personalId = string.IsNullOrWhiteSpace(model.PersonalId) ? null : model.PersonalId.Trim();

            if (jobNumber != null && await _userManager.Users.AnyAsync(u => u.JobNumber == jobNumber && u.Id != id))
            {
                ModelState.AddModelError(string.Empty, "الرقم الوظيفي مستخدم بالفعل لموظف آخر");
            }

            if (personalId != null && await _userManager.Users.AnyAsync(u => u.PersonalId == personalId && u.Id != id))
            {
                ModelState.AddModelError(string.Empty, "الرقم الشخصي مستخدم بالفعل لموظف آخر");
            }

            if (!ModelState.IsValid)
            {
                await ReloadEditUserViewBagsAsync(id, role);
                return View(user);
            }

            user.FullName = model.FullName;
            user.JobNumber = jobNumber;
            user.PersonalId = personalId;
            user.Responsibilities = model.Responsibilities;
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

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
                    _fileUpload.DeleteFile(user.ProfilePhotoPath);

                var photoResult = await _fileUpload.SaveFileAsync(profilePhoto, $"users/{user.Id}");
                if (photoResult.Success)
                    user.ProfilePhotoPath = photoResult.FilePath;
                else
                    ModelState.AddModelError(string.Empty, photoResult.ErrorMessage ?? "فشل رفع الصورة");
            }

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

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "تعديل", "ApplicationUser", user.Id, $"تعديل بيانات {user.FullName}");
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

            await _auditService.LogAsync(_userManager.GetUserId(User)!, user.IsActive ? "تفعيل" : "تعطيل", "ApplicationUser", user.Id, user.IsActive ? "تم تفعيل الحساب" : "تم تعطيل الحساب");
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

            if (result.Succeeded)
                await _auditService.LogAsync(_userManager.GetUserId(User)!, "إعادة تعيين كلمة المرور", "ApplicationUser", user.Id, "تم تحديث كلمة المرور");

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? $"تم تحديث كلمة مرور {user.FullName} بنجاح"
                : string.Join("، ", result.Errors.Select(e => e.Description));

            return RedirectToAction("EditUser", new { id });
        }
        [HttpGet]
        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound();

            ViewBag.Positions = await _context.EmployeePositions
                .Include(p => p.Department)
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.IsPrimary).ThenByDescending(p => p.StartDate)
                .ToListAsync();

            ViewBag.EffectivePermissions = await _permissionService.GetEffectivePermissionsAsync(id);

            ViewBag.Documents = await _context.EmployeeDocuments
                .Where(d => d.UserId == id)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            var auditLogs = await _context.AuditLogs
                .Where(a => a.EntityName == "ApplicationUser" && a.EntityId == id)
                .OrderByDescending(a => a.Timestamp)
                .Take(30)
                .ToListAsync();
            ViewBag.AuditLogs = auditLogs;

            var actorIds = auditLogs.Select(a => a.UserId).Distinct().ToList();
            ViewBag.AuditActors = await _userManager.Users
                .Where(u => actorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            return View(user);
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