using AtharERP_System.Models.Entities;
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
        // UserManager = يدير المستخدمين (إنشاء، حذف، تعديل، التحقق من كلمة المرور)
        // SignInManager = يدير الجلسة (تسجيل دخول، خروج، كوكيز)
        // RoleManager = يدير الأدوار (إنشاء، حذف، تعديل)
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        // Constructor: يستقبل الخدمات من ASP.NET Core تلقائياً (Dependency Injection)
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // ============================================
        // دالة 1: Login (GET) — عرض صفحة الدخول
        // ============================================
        // [HttpGet] = تستجيب لطلب المتصفح العادي (فتح رابط)
        // returnUrl = إذا حاول المستخدم فتح صفحة محمية قبل الدخول، يُعاد إليها بعد الدخول
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

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
        // دالة 2: Login (POST) — معالجة بيانات الدخول
        // ============================================
        // [HttpPost] = تستجيب لإرسال النموذج (Form submit)
        // [ValidateAntiForgeryToken] = يتحقق أن الطلب جاء من موقعك (يمنع هجوم CSRF)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            // 1. البحث عن المستخدم بالبريد الإلكتروني
            var user = await _userManager.FindByEmailAsync(email);

            // 2. التحقق من وجود المستخدم ونشاطه
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة أو الحساب معطل");
                return View(); // يُعيد الصفحة مع رسالة خطأ
            }

            // 3. التحقق من كلمة المرور وتسجيل الدخول
            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // 4. تحديث آخر دخول
                user.LastLogin = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // 5. إعادة التوجيه للصفحة المطلوبة أو الرئيسية
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            // 6. إذا فشلت كلمة المرور
            ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة");
            return View();
        }

        // ============================================
        // دالة 3: Register (GET) — صفحة إضافة مستخدم
        // ============================================
        // [Authorize(Roles = "مدير النظام")] = فقط المدير يمكنه الوصول
        [HttpGet]
        [Authorize(Roles = "مدير النظام")]
        public async Task<IActionResult> Register()
        {
            // نجلب الأدوار النشطة لعرضها في قائمة منسدلة
            ViewBag.Roles = await _roleManager.Roles.Where(r => r.IsActive).Select(r => r.Name).ToListAsync();
            return View();
        }

        // ============================================
        // دالة 4: Register (POST) — إنشاء المستخدم
        // ============================================
        [HttpPost]
        [Authorize(Roles = "مدير النظام")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string fullName,
            string email,
            string password,
            string confirmPassword,
            string? jobTitle,
            string? department,
            EngineerRank engineerRank,
            string? phoneNumber,
            string? expectedLocationName,
            double? expectedLatitude,
            double? expectedLongitude,
            double? allowedRadiusMeters,
            string role)
        {
            // إعادة تحميل الأدوار في حال فشل التحقق
            ViewBag.Roles = await _roleManager.Roles.Where(r => r.IsActive).Select(r => r.Name).ToListAsync();

            // التحقق من تطابق كلمة المرور
            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "كلمتا المرور غير متطابقتين");
                return View();
            }

            // التحقق من عدم تكرار البريد
            if (await _userManager.FindByEmailAsync(email) != null)
            {
                ModelState.AddModelError(string.Empty, "البريد الإلكتروني مستخدم بالفعل");
                return View();
            }

            // إنشاء كائن المستخدم
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                JobTitle = jobTitle,
                Department = department,
                EngineerRank = engineerRank,
                PhoneNumber = phoneNumber,
                ExpectedLocationName = expectedLocationName,
                ExpectedLatitude = expectedLatitude,
                ExpectedLongitude = expectedLongitude,
                AllowedRadiusMeters = allowedRadiusMeters,
                EmailConfirmed = true,
                IsActive = true,
                JoinDate = DateTime.UtcNow
            };

            // إنشاء المستخدم في قاعدة البيانات
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // إضافة الدور المختار
                if (!string.IsNullOrEmpty(role))
                    await _userManager.AddToRoleAsync(user, role);

                TempData["Success"] = $"تم إنشاء المستخدم {fullName} بنجاح";
                return RedirectToAction("Users", "Admin");
            }

            // إذا حدث خطأ في الإنشاء
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View();
        }

        // ============================================
        // دالة 5: Logout — تسجيل الخروج
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // ============================================
        // دالة 6: AccessDenied — وصول مرفوض
        // ============================================
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}