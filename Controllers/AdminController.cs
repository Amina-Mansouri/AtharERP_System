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
        public async Task<IActionResult> Users(string? q, int? departmentId, int? rankId, int? trackId, string? role, string status = "all", int page = 1)
        {
            const int pageSize = 20;

            var query = _userManager.Users
    .Include(u => u.Department).ThenInclude(d => d!.ParentDepartment)
    .Include(u => u.JobRankRef).ThenInclude(r => r!.CareerTrack)
    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(u => u.JobNumber != null && u.JobNumber.Contains(q));

            if (departmentId.HasValue)
                query = query.Where(u => u.DepartmentId == departmentId);

            if (rankId.HasValue)
                query = query.Where(u => u.JobRankId == rankId);

            if (trackId.HasValue)
                query = query.Where(u => u.JobRankRef!.CareerTrackId == trackId);

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
    .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
    .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);

            ViewBag.UserRoles = userRoles;
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.Roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            ViewBag.RoleOptions = await _roleManager.Roles.OrderBy(r => r.Name).Select(r => r.Name).ToListAsync();

            var soonCutoff = DateTime.UtcNow.AddDays(30);
            ViewBag.TotalEmployees = await _userManager.Users.CountAsync();
            ViewBag.TotalActiveEmployees = await _userManager.Users.CountAsync(u => u.IsActive);
            ViewBag.TotalContractsSoon = await _userManager.Users.CountAsync(u => u.ContractEndDate != null && u.ContractEndDate <= soonCutoff && u.ContractEndDate >= DateTime.UtcNow && !u.IsSuspended);
            ViewBag.TotalContractsExpired = await _userManager.Users.CountAsync(u => u.IsSuspended);
            ViewBag.CurrentQ = q;
            ViewBag.CurrentDepartmentId = departmentId;
            ViewBag.CurrentRank = rankId;
            ViewBag.CurrentTrack = trackId;
            ViewBag.CareerTracks = await _context.CareerTracks.OrderBy(t => t.DisplayOrder).ToListAsync();
            ViewBag.JobRanks = await _context.JobRanks.OrderBy(r => r.DisplayOrder).ToListAsync();
            ViewBag.CurrentRoleFilter = role;
            ViewBag.CurrentStatus = status;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(
    string id,
     [Bind("FirstName,LastName,JobNumber,NextOfKinPhone,DepartmentId,JobRankId,ContractSalary,ContractStartDate,ContractEndDate,PhoneNumber,ExpectedLocationName,ExpectedLatitude,ExpectedLongitude,AllowedRadiusMeters")] ApplicationUser model,
    IFormFile? profilePhoto,
    IFormFile? contractImage,
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

            if (jobNumber != null && await _userManager.Users.AnyAsync(u => u.JobNumber == jobNumber && u.Id != id))
            {
                ModelState.AddModelError(string.Empty, "الرقم الوظيفي مستخدم بالفعل لموظف آخر");
            }

            bool contractDatesChanging = user.ContractStartDate != model.ContractStartDate || user.ContractEndDate != model.ContractEndDate;
            bool contractImageReplacing = contractImage != null && contractImage.Length > 0 && !string.IsNullOrEmpty(user.ContractImagePath);
            bool isContractRenewal = contractDatesChanging || contractImageReplacing;

            if (model.ContractStartDate.HasValue && model.ContractEndDate.HasValue && model.ContractEndDate.Value.Date <= model.ContractStartDate.Value.Date)
            {
                ModelState.AddModelError(string.Empty, "تاريخ نهاية العقد يجب أن يكون بعد تاريخ بدايته");
            }

            if (isContractRenewal && model.ContractEndDate.HasValue && model.ContractEndDate.Value.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(string.Empty, "تاريخ نهاية العقد لا يمكن أن يكون تاريخاً منتهياً عند تجديد العقد أو رفع صورة عقد جديدة");
            }

            if (contractDatesChanging && model.ContractEndDate.HasValue && user.ContractEndDate.HasValue && model.ContractEndDate.Value.Date == user.ContractEndDate.Value.Date)
            {
                ModelState.AddModelError(string.Empty, "تاريخ نهاية العقد الجديد يطابق تاريخ النهاية السابق — التجديد يجب أن يمدّد العقد فعلياً بتاريخ نهاية مختلف");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("UserDetails", new { id });
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.JobNumber = jobNumber;
            user.NextOfKinPhone = model.NextOfKinPhone;
            user.DepartmentId = model.DepartmentId;
            user.JobRankId = model.JobRankId;
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

            // تجديد حقيقي (تغيّر التواريخ): أرشفة العقد القديم كاملاً بدل حذفه (سجل العقود)
            bool hadPreviousContract = user.ContractStartDate.HasValue || user.ContractEndDate.HasValue || !string.IsNullOrEmpty(user.ContractImagePath);

            if (hadPreviousContract && contractDatesChanging)
            {
                _context.ContractHistories.Add(new ContractHistory
                {
                    UserId = user.Id,
                    ContractStartDate = user.ContractStartDate,
                    ContractEndDate = user.ContractEndDate,
                    ContractSalary = user.ContractSalary,
                    ContractImagePath = user.ContractImagePath,
                    ArchivedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            user.ContractSalary = model.ContractSalary;
            user.ContractStartDate = model.ContractStartDate;
            user.ContractEndDate = model.ContractEndDate;
            if (user.IsSuspended && user.ContractEndDate.HasValue && user.ContractEndDate.Value.Date > DateTime.UtcNow.Date)
            {
                user.IsSuspended = false;
                user.SuspendedReason = null;
            }

            if (contractImage != null && contractImage.Length > 0)
            {
                // نفس فترة العقد ولم تُؤرشف أعلاه: هذا استبدال ملف خاطئ فقط، فيُحذف نهائياً بلا أرشفة
                if (!contractDatesChanging && !string.IsNullOrEmpty(user.ContractImagePath))
                {
                    _fileUpload.DeleteFile(user.ContractImagePath);
                }

                var contractResult = await _fileUpload.SaveFileAsync(contractImage, $"contracts/{user.Id}");
                if (contractResult.Success)
                    user.ContractImagePath = contractResult.FilePath;
                else
                    ModelState.AddModelError(string.Empty, contractResult.ErrorMessage ?? "فشل رفع صورة العقد");
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" — ", result.Errors.Select(e => e.Description));
                return RedirectToAction("UserDetails", new { id });
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
            return RedirectToAction("UserDetails", new { id });
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

            return RedirectToAction("UserDetails", new { id });
        }
        [HttpGet]
        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.Users
      .Include(u => u.Department)
      .Include(u => u.JobRankRef).ThenInclude(r => r!.CareerTrack)
      .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound();


            ViewBag.EffectivePermissions = await _permissionService.GetEffectivePermissionsAsync(id);

            ViewBag.Documents = await _context.EmployeeDocuments
                .Where(d => d.UserId == id)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            ViewBag.ContractHistory = await _context.ContractHistories
                .Where(c => c.UserId == id)
                .OrderByDescending(c => c.ArchivedAt)
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
    .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);

            var currentRoles = await _userManager.GetRolesAsync(user);
            ViewBag.CurrentRole = currentRoles.FirstOrDefault();
            ViewBag.RoleOptions = await _roleManager.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).Select(r => r.Name).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.CareerTracks = await _context.CareerTracks.OrderBy(t => t.DisplayOrder).ToListAsync();
            ViewBag.JobRanks = await _context.JobRanks.OrderBy(r => r.DisplayOrder).ToListAsync();

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.RoleOptions = await _roleManager.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).Select(r => r.Name).ToListAsync();
            ViewBag.CareerTracks = await _context.CareerTracks.OrderBy(t => t.DisplayOrder).ToListAsync();
            ViewBag.JobRanks = await _context.JobRanks.OrderBy(r => r.DisplayOrder).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(
            string firstName,
            string lastName,
            string email,
            string password,
            string confirmPassword,
            string? jobNumber,
            string? nextOfKinPhone,
            IFormFile? profilePhoto,
            IFormFile? contractImage,
            int? departmentId,
            int jobRankId,
            decimal contractSalary,
            DateTime? contractStartDate,
            DateTime? contractEndDate,
            string? phoneNumber,
            string? expectedLocationName,
            double? expectedLatitude,
            double? expectedLongitude,
            int? allowedRadiusMeters,
            string role)
        {
            async Task<IActionResult> FailAsync(string error)
            {
                ViewBag.Error = error;
                ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
                ViewBag.RoleOptions = await _roleManager.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).Select(r => r.Name).ToListAsync();
                ViewBag.CareerTracks = await _context.CareerTracks.OrderBy(t => t.DisplayOrder).ToListAsync();
                ViewBag.JobRanks = await _context.JobRanks.OrderBy(r => r.DisplayOrder).ToListAsync();

                ViewBag.PostedFirstName = firstName;
                ViewBag.PostedLastName = lastName;
                ViewBag.PostedEmail = email;
                ViewBag.PostedPassword = password;
                ViewBag.PostedConfirmPassword = confirmPassword;
                ViewBag.PostedJobNumber = jobNumber;
                ViewBag.PostedNextOfKinPhone = nextOfKinPhone;
                ViewBag.PostedDepartmentId = departmentId;
                ViewBag.PostedJobRankId = jobRankId;
                ViewBag.PostedContractSalary = contractSalary;
                ViewBag.PostedContractStartDate = contractStartDate;
                ViewBag.PostedContractEndDate = contractEndDate;
                ViewBag.PostedPhoneNumber = phoneNumber;
                ViewBag.PostedExpectedLocationName = expectedLocationName;
                ViewBag.PostedExpectedLatitude = expectedLatitude;
                ViewBag.PostedExpectedLongitude = expectedLongitude;
                ViewBag.PostedAllowedRadiusMeters = allowedRadiusMeters;
                ViewBag.PostedRole = role;

                if (departmentId.HasValue)
                {
                    var deptForRestore = await _context.Departments.FindAsync(departmentId.Value);
                    ViewBag.PostedDeptParentId = deptForRestore?.ParentDepartmentId ?? deptForRestore?.Id;
                }
                if (jobRankId > 0)
                {
                    var rankForRestore = await _context.JobRanks.FindAsync(jobRankId);
                    ViewBag.PostedTrackId = rankForRestore?.CareerTrackId;
                }

                return View();
            }

            if (password != confirmPassword)
                return await FailAsync("كلمتا المرور غير متطابقتين");

            if (await _userManager.FindByEmailAsync(email) != null)
                return await FailAsync("البريد الإلكتروني مستخدم بالفعل");

            if (departmentId == null)
                return await FailAsync("القسم مطلوب");

            jobNumber = string.IsNullOrWhiteSpace(jobNumber) ? null : jobNumber.Trim();

            if (jobNumber != null && await _userManager.Users.AnyAsync(u => u.JobNumber == jobNumber))
                return await FailAsync("الرقم الوظيفي مستخدم بالفعل لموظف آخر");

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                JobNumber = jobNumber,
                NextOfKinPhone = nextOfKinPhone,
                DepartmentId = departmentId,
                JobRankId = jobRankId,
                ContractSalary = contractSalary,
                ContractStartDate = contractStartDate,
                ContractEndDate = contractEndDate,
                PhoneNumber = phoneNumber,
                ExpectedLocationName = expectedLocationName,
                ExpectedLatitude = expectedLatitude,
                ExpectedLongitude = expectedLongitude,
                AllowedRadiusMeters = allowedRadiusMeters ?? 100,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
                return await FailAsync(string.Join(" · ", result.Errors.Select(e => e.Description)));

            if (!string.IsNullOrEmpty(role))
                await _userManager.AddToRoleAsync(user, role);

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                var photoResult = await _fileUpload.SaveFileAsync(profilePhoto, $"users/{user.Id}");
                if (photoResult.Success)
                {
                    user.ProfilePhotoPath = photoResult.FilePath;
                    await _userManager.UpdateAsync(user);
                }
            }
            if (contractImage != null && contractImage.Length > 0)
            {
                var contractResult = await _fileUpload.SaveFileAsync(contractImage, $"contracts/{user.Id}");
                if (contractResult.Success)
                {
                    user.ContractImagePath = contractResult.FilePath;
                    await _userManager.UpdateAsync(user);
                }
            }

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "إضافة موظف", "ApplicationUser", user.Id, $"إنشاء موظف {firstName} {lastName}");
            TempData["Success"] = $"تم إنشاء الموظف {firstName} {lastName} بنجاح";
            return RedirectToAction("Users");
        }
        // ============================================
        // إدارة الأدوار
        // ============================================

        [HttpGet]
        public async Task<IActionResult> Roles(string? roleId)
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

            var selectedRole = !string.IsNullOrEmpty(roleId)
                ? roles.FirstOrDefault(r => r.Id == roleId)
                : roles.FirstOrDefault();

            if (selectedRole != null)
            {
                ViewBag.SelectedRole = selectedRole;
                ViewBag.AllPermissions = await _context.Permissions
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module).ThenBy(p => p.Code)
                    .ToListAsync();
                ViewBag.SelectedPermissionIds = await _context.RolePermissions
                    .Where(rp => rp.RoleId == selectedRole.Id && rp.IsGranted)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();
            }

            return View(roles);
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
                TempData["Error"] = "بيانات غير صحيحة، تأكدي من إدخال اسم الدور";
                return RedirectToAction("Roles");
            }

            model.CreatedAt = DateTime.UtcNow;
            model.CanDelete = true;
            var result = await _roleManager.CreateAsync(model);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" — ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Roles");
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

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "إضافة دور", "ApplicationRole", model.Id, $"إنشاء دور {model.Name}");
            TempData["Success"] = $"تم إنشاء الدور {model.Name} بنجاح";
            return RedirectToAction("Roles");
        }

        [HttpGet]
        public IActionResult EditRole(string id)
        {
            // الشاشة المدمجة في Roles.cshtml (اختيار الدور من القائمة) تغطي نفس الوظيفة الآن
            return RedirectToAction("Roles", new { roleId = id });
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

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "تعديل دور", "ApplicationRole", id, $"تعديل دور {role.Name}");
            TempData["Success"] = $"تم تحديث الدور {role.Name} بنجاح";
            return RedirectToAction("Roles");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoleDetails(string id, string name, string? description, bool isActive)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            if (role.IsTemplate && !string.Equals(name, role.Name, StringComparison.Ordinal))
            {
                TempData["Error"] = "لا يمكن تغيير اسم دور قالب جاهز";
                return RedirectToAction("Roles", new { roleId = id });
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "اسم الدور مطلوب";
                return RedirectToAction("Roles", new { roleId = id });
            }

            role.Name = name.Trim();
            role.Description = description;
            role.IsActive = isActive;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" — ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Roles", new { roleId = id });
            }

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "تعديل دور", "ApplicationRole", id, $"تعديل بيانات دور {role.Name}");
            TempData["Success"] = $"تم تحديث بيانات الدور {role.Name} بنجاح";
            return RedirectToAction("Roles", new { roleId = id });
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

            var deletedRoleName = role.Name;
            await _roleManager.DeleteAsync(role);

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "حذف دور", "ApplicationRole", id, $"حذف دور {deletedRoleName}");
            TempData["Success"] = $"تم حذف الدور {deletedRoleName} بنجاح";
            return RedirectToAction("Roles");
        }

        // ============================================
        // إدارة الأقسام (Department)
        // ============================================

        [HttpGet]
        public async Task<IActionResult> Departments(int? selectedId)
        {
            var departments = await _context.Departments
                .Include(d => d.ParentDepartment)
                .Include(d => d.Users)
                .OrderBy(d => d.ParentDepartmentId == null ? 0 : 1)
                .ThenBy(d => d.Name)
                .ToListAsync();

            var selected = selectedId.HasValue
                ? departments.FirstOrDefault(d => d.Id == selectedId.Value)
                : departments.FirstOrDefault(d => d.ParentDepartmentId == null);

            if (selected != null)
            {
                var childIds = departments.Where(d => d.ParentDepartmentId == selected.Id).Select(d => d.Id).ToList();
                var scopeIds = new List<int> { selected.Id }.Concat(childIds).ToList();

                ViewBag.SelectedDepartment = selected;
                ViewBag.SelectedChildCount = childIds.Count;
                ViewBag.SelectedEmployees = await _userManager.Users
                    .Where(u => u.DepartmentId != null && scopeIds.Contains(u.DepartmentId.Value) && u.IsActive)
                    .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                    .ToListAsync();
            }

            return View(departments);
        }

       

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment([Bind("Name,ParentDepartmentId,Description")] Department model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة، تأكدي من إدخال الاسم";
                return RedirectToAction("Departments");
            }

            model.IsActive = true;
            model.CreatedAt = DateTime.UtcNow;

            _context.Departments.Add(model);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "إضافة قسم", "Department", model.Id.ToString(), $"إنشاء قسم {model.Name}");
            TempData["Success"] = $"تم إنشاء القسم {model.Name} بنجاح";
            return RedirectToAction("Departments");
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
                TempData["Error"] = "بيانات غير صحيحة، تأكدي من إدخال الاسم";
                return RedirectToAction("Departments");
            }

            department.Name = model.Name;
            department.ParentDepartmentId = model.ParentDepartmentId;
            department.Description = model.Description;
            department.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "تعديل قسم", "Department", id.ToString(), $"تعديل قسم {department.Name}");
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

            var deletedDeptName = department.Name;
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "حذف قسم", "Department", id.ToString(), $"حذف قسم {deletedDeptName}");
            TempData["Success"] = "تم حذف القسم بنجاح";
            return RedirectToAction("Departments");
        }
        // ============================================
        // إدارة المسارات والرتب (CareerTrack / JobRank)
        // ============================================

        [HttpGet]
        public async Task<IActionResult> RanksAndTracks()
        {
            var tracks = await _context.CareerTracks
                .Include(t => t.Ranks)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            return View(tracks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrack(string code, string nameAr, string? nameEn, int displayOrder)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nameAr))
            {
                TempData["Error"] = "الرمز واسم المسار مطلوبان";
                return RedirectToAction("RanksAndTracks");
            }

            _context.CareerTracks.Add(new CareerTrack { Code = code.Trim(), NameAr = nameAr.Trim(), NameEn = nameEn, DisplayOrder = displayOrder });
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "إضافة مسار", "CareerTrack", code, $"إنشاء مسار {nameAr}");
            TempData["Success"] = $"تم إنشاء المسار {nameAr} بنجاح";
            return RedirectToAction("RanksAndTracks");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrack(int id, string code, string nameAr, string? nameEn, int displayOrder)
        {
            var track = await _context.CareerTracks.FindAsync(id);
            if (track == null) return NotFound();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nameAr))
            {
                TempData["Error"] = "الرمز واسم المسار مطلوبان";
                return RedirectToAction("RanksAndTracks");
            }

            track.Code = code.Trim();
            track.NameAr = nameAr.Trim();
            track.NameEn = nameEn;
            track.DisplayOrder = displayOrder;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "تعديل مسار", "CareerTrack", id.ToString(), $"تعديل مسار {track.NameAr}");
            TempData["Success"] = $"تم تحديث المسار {track.NameAr} بنجاح";
            return RedirectToAction("RanksAndTracks");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrack(int id)
        {
            var track = await _context.CareerTracks.Include(t => t.Ranks).FirstOrDefaultAsync(t => t.Id == id);
            if (track == null) return NotFound();

            if (track.Ranks.Any())
            {
                TempData["Error"] = "لا يمكن حذف المسار لوجود رتب مرتبطة به";
                return RedirectToAction("RanksAndTracks");
            }

            var deletedName = track.NameAr;
            _context.CareerTracks.Remove(track);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "حذف مسار", "CareerTrack", id.ToString(), $"حذف مسار {deletedName}");
            TempData["Success"] = "تم حذف المسار بنجاح";
            return RedirectToAction("RanksAndTracks");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRank(int careerTrackId, string code, string nameAr, string? nameEn, int displayOrder, decimal? baseSalary)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nameAr))
            {
                TempData["Error"] = "الرمز واسم الرتبة مطلوبان";
                return RedirectToAction("RanksAndTracks");
            }

            _context.JobRanks.Add(new JobRank
            {
                CareerTrackId = careerTrackId,
                Code = code.Trim(),
                NameAr = nameAr.Trim(),
                NameEn = nameEn,
                DisplayOrder = displayOrder,
                BaseSalary = baseSalary
            });
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "إضافة رتبة", "JobRank", code, $"إنشاء رتبة {nameAr}");
            TempData["Success"] = $"تم إنشاء الرتبة {nameAr} بنجاح";
            return RedirectToAction("RanksAndTracks");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRank(int id, int careerTrackId, string code, string nameAr, string? nameEn, int displayOrder, decimal? baseSalary)
        {
            var rank = await _context.JobRanks.FindAsync(id);
            if (rank == null) return NotFound();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nameAr))
            {
                TempData["Error"] = "الرمز واسم الرتبة مطلوبان";
                return RedirectToAction("RanksAndTracks");
            }

            rank.CareerTrackId = careerTrackId;
            rank.Code = code.Trim();
            rank.NameAr = nameAr.Trim();
            rank.NameEn = nameEn;
            rank.DisplayOrder = displayOrder;
            rank.BaseSalary = baseSalary;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "تعديل رتبة", "JobRank", id.ToString(), $"تعديل رتبة {rank.NameAr}");
            TempData["Success"] = $"تم تحديث الرتبة {rank.NameAr} بنجاح";
            return RedirectToAction("RanksAndTracks");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRank(int id)
        {
            var rank = await _context.JobRanks.FindAsync(id);
            if (rank == null) return NotFound();

            var hasEmployees = await _userManager.Users.AnyAsync(u => u.JobRankId == id);
            if (hasEmployees)
            {
                TempData["Error"] = "لا يمكن حذف الرتبة لوجود موظفين مرتبطين بها";
                return RedirectToAction("RanksAndTracks");
            }

            var deletedName = rank.NameAr;
            _context.JobRanks.Remove(rank);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(_userManager.GetUserId(User)!, "حذف رتبة", "JobRank", id.ToString(), $"حذف رتبة {deletedName}");
            TempData["Success"] = "تم حذف الرتبة بنجاح";
            return RedirectToAction("RanksAndTracks");
        }
    }
}