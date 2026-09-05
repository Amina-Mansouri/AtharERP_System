using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    // بوابة دخول ولوحة تحكم المقاول — منفصلة تماماً عن Identity الخاص بموظفي الشركة
    public class ContractorPortalController : Controller
    {
        private readonly AppDbContext _context;

        public ContractorPortalController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true && User.Identity.AuthenticationType == "ContractorScheme")
                return RedirectToAction("Dashboard");

            ViewData["HideNav"] = true;
            ViewData["AuthTheme"] = "ds";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            ViewData["HideNav"] = true;
            ViewData["AuthTheme"] = "ds";

            var contractor = await _context.Contractors.FirstOrDefaultAsync(c => c.Email == email);

            if (contractor == null || !contractor.IsActive)
            {
                ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة");
                return View();
            }

            var result = new PasswordHasher<Contractor>().VerifyHashedPassword(contractor, contractor.PasswordHash, password ?? string.Empty);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, contractor.Id.ToString()),
                new Claim(ClaimTypes.Name, contractor.Name),
                new Claim(ClaimTypes.Email, contractor.Email)
            };

            var identity = new ClaimsIdentity(claims, "ContractorScheme");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("ContractorScheme", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            });

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ContractorScheme")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("ContractorScheme");
            return RedirectToAction("Login");
        }

        [Authorize(AuthenticationSchemes = "ContractorScheme")]
        public async Task<IActionResult> Dashboard()
        {
            var contractorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var assignments = await _context.SiteContractors
                .Include(sa => sa.Site).ThenInclude(s => s.Project)
                .Where(sa => sa.ContractorId == contractorId)
                .ToListAsync();

            ViewData["HideNav"] = true;
            ViewBag.ContractorName = User.FindFirstValue(ClaimTypes.Name);
            return View(assignments);
        }
    }
}