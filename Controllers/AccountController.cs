using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    // [AllowAnonymous] = أي شخص يمكنه الدخول حتى لو لم يسجل
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            AppDbContext context,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _emailSender = emailSender;
        }

        // ============================================
        // Login (GET)
        // ============================================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ============================================
        // Login (POST)
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة أو الحساب معطل");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.LastLogin = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "تم قفل الحساب مؤقتاً بسبب تكرار محاولات الدخول الفاشلة. حاول مرة أخرى بعد 15 دقيقة");
                return View();
            }

            ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة");
            return View();
        }

        // ============================================
        // Register (GET)
        // ============================================
        [HttpGet]
        [Authorize(Roles = "مدير النظام")]
        public async Task<IActionResult> Register()
        {
            await LoadRegisterDropdownsAsync();
            return View();
        }

        // ============================================
        // Register (POST)
        // ============================================
        [HttpPost]
        [Authorize(Roles = "مدير النظام")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string fullName,
            string email,
            string password,
            string confirmPassword,
            string? jobNumber,
            string? personalId,
            int? departmentId,
            JobRank rank,
            CareerTrack careerTrack,
            decimal contractSalary,
            DateTime? contractStartDate,
            string? phoneNumber,
            string? expectedLocationName,
            double? expectedLatitude,
            double? expectedLongitude,
            int? allowedRadiusMeters,
            string role)
        {
            await LoadRegisterDropdownsAsync();

            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "كلمتا المرور غير متطابقتين");
                return View();
            }

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                ModelState.AddModelError(string.Empty, "البريد الإلكتروني مستخدم بالفعل");
                return View();
            }

            // قاعدة العمل 3: كل موظف يجب أن يكون مرتبطاً بقسم واحد على الأقل
            if (departmentId == null)
            {
                ModelState.AddModelError(string.Empty, "القسم مطلوب");
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                JobNumber = jobNumber,
                PersonalId = personalId,
                DepartmentId = departmentId,
                Rank = rank,
                CareerTrack = careerTrack,
                ContractSalary = contractSalary,
                ContractStartDate = contractStartDate,
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

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(role))
                    await _userManager.AddToRoleAsync(user, role);

                _context.EmployeePositions.Add(new EmployeePosition
                {
                    UserId = user.Id,
                    DepartmentId = departmentId.Value,
                    Rank = rank,
                    Track = careerTrack,
                    StartDate = contractStartDate ?? DateTime.UtcNow,
                    IsPrimary = true
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = $"تم إنشاء المستخدم {fullName} بنجاح";
                return RedirectToAction("Users", "Admin");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View();
        }

        // ============================================
        // Forgot Password (GET)
        // ============================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // ============================================
        // Forgot Password (POST)
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null && user.IsActive)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetUrl = Url.Action("ResetPassword", "Account",
                    new { email = user.Email, token }, protocol: Request.Scheme);

                var body = $"<p>لإعادة تعيين كلمة المرور الخاصة بك في نظام أثر، اضغط على الرابط التالي:</p>" +
                           $"<p><a href=\"{resetUrl}\">إعادة تعيين كلمة المرور</a></p>";

                await _emailSender.SendEmailAsync(user.Email!, "إعادة تعيين كلمة المرور - أثر", body);
            }

            // نفس الرسالة سواء وُجد البريد أم لا، لمنع تسريب معلومات عن الحسابات الموجودة
            TempData["Success"] = "إذا كان البريد الإلكتروني مسجلاً لدينا، فسيصلك رابط لإعادة تعيين كلمة المرور";
            return RedirectToAction("Login");
        }

        // ============================================
        // Reset Password (GET)
        // ============================================
        [HttpGet]
        public IActionResult ResetPassword(string? email = null, string? token = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        // ============================================
        // Reset Password (POST)
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string token, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "كلمتا المرور غير متطابقتين");
                ViewBag.Email = email;
                ViewBag.Token = token;
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // لا نكشف عدم وجود المستخدم
                TempData["Success"] = "تم تحديث كلمة المرور بنجاح";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = "تم تحديث كلمة المرور بنجاح، يمكنك الآن تسجيل الدخول";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        // ============================================
        // Logout
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // ============================================
        // AccessDenied
        // ============================================
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ============================================
        // دالة مساعدة
        // ============================================
        private async Task LoadRegisterDropdownsAsync()
        {
            ViewBag.Roles = await _roleManager.Roles.Where(r => r.IsActive).Select(r => r.Name).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.JobRanks = EnumDisplayHelper.GetDisplayList<JobRank>();
            ViewBag.CareerTracks = EnumDisplayHelper.GetDisplayList<CareerTrack>();
        }
    }
}