using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly FileUploadService _fileUpload;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            AppDbContext context,
            IEmailSender emailSender,
            FileUploadService fileUpload)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _emailSender = emailSender;
            _fileUpload = fileUpload;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, double? latitude, double? longitude, string? returnUrl = null)
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
                if (user.ExpectedLatitude.HasValue && user.ExpectedLongitude.HasValue)
                {
                    if (!latitude.HasValue || !longitude.HasValue)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "يلزم السماح بتحديد الموقع في المتصفح لتسجيل الدخول");
                        return View();
                    }

                    var distance = GeoHelper.CalculateDistance(user.ExpectedLatitude.Value, user.ExpectedLongitude.Value, latitude.Value, longitude.Value);
                    if (distance > user.AllowedRadiusMeters)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "تعذّر تسجيل الدخول: موقعك الحالي خارج النطاق المسموح به");
                        return View();
                    }
                }

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

        [HttpPost]
        [Authorize(Roles = "مدير النظام")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
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
    JobRank rank,
    CareerTrack careerTrack,
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
            // الشاشة الوحيدة التي تستدعي هذا الإجراء (Users.cshtml) ترسله عبر AJAX وتقرأ استجابة JSON فقط
            if (password != confirmPassword)
                return Json(new { success = false, message = "كلمتا المرور غير متطابقتين" });

            if (await _userManager.FindByEmailAsync(email) != null)
                return Json(new { success = false, message = "البريد الإلكتروني مستخدم بالفعل" });

            if (departmentId == null)
                return Json(new { success = false, message = "القسم مطلوب" });

            // تطبيع الحقول الفارغة إلى null لتفادي تعارض القيم الفارغة مع قيد التفرّد
            jobNumber = string.IsNullOrWhiteSpace(jobNumber) ? null : jobNumber.Trim();

            if (jobNumber != null && await _userManager.Users.AnyAsync(u => u.JobNumber == jobNumber))
                return Json(new { success = false, message = "الرقم الوظيفي مستخدم بالفعل لموظف آخر" });

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                JobNumber = jobNumber,
                NextOfKinPhone = nextOfKinPhone,
                DepartmentId = departmentId,
                Rank = rank,
                CareerTrack = careerTrack,
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
                return Json(new { success = false, message = string.Join(" · ", result.Errors.Select(e => e.Description)) });

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

            return Json(new { success = true, message = $"تم إنشاء الموظف {firstName} {lastName} بنجاح" });
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

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

            TempData["Success"] = "إذا كان البريد الإلكتروني مسجلاً لدينا، فسيصلك رابط لإعادة تعيين كلمة المرور";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string? email = null, string? token = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task LoadRegisterDropdownsAsync()
        {
            ViewBag.Roles = await _roleManager.Roles.Where(r => r.IsActive).Select(r => r.Name).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.JobRanks = EnumDisplayHelper.GetDisplayList<JobRank>();
            ViewBag.CareerTracks = EnumDisplayHelper.GetDisplayList<CareerTrack>();
        }
    }
}