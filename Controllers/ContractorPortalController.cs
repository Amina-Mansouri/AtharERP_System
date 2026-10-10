using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authentication;
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
        private readonly FileUploadService _fileUpload;

        public ContractorPortalController(AppDbContext context, FileUploadService fileUpload)
        {
            _context = context;
            _fileUpload = fileUpload;
        }

        private int CurrentContractorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<bool> CanAccessSiteAsync(int siteId)
        {
            return await _context.SiteContractors.AnyAsync(sa => sa.SiteId == siteId && sa.ContractorId == CurrentContractorId);
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
            var contractorId = CurrentContractorId;

            var assignments = await _context.SiteContractors
                .Include(sa => sa.Site).ThenInclude(s => s.Project)
                .Include(sa => sa.Site).ThenInclude(s => s.Operations)
                .Where(sa => sa.ContractorId == contractorId)
                .ToListAsync();

            ViewData["PlainPage"] = true;
            ViewBag.ContractorName = User.FindFirstValue(ClaimTypes.Name);
            return View(assignments);
        }

        [Authorize(AuthenticationSchemes = "ContractorScheme")]
        public async Task<IActionResult> SiteDetails(int siteId)
        {
            if (!await CanAccessSiteAsync(siteId))
                return Forbid();

            var site = await _context.Sites
                .Include(s => s.Operations)
                .FirstOrDefaultAsync(s => s.Id == siteId);
            if (site == null)
                return NotFound();

            var requirements = await _context.SiteRequirements
                .Include(r => r.SentBy)
                .Include(r => r.SentByContractor)
                .Where(r => r.SiteId == siteId)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            var unreadFromCompany = requirements.Where(r => !r.IsFromContractor && !r.IsRead).ToList();
            foreach (var r in unreadFromCompany)
                r.IsRead = true;
            if (unreadFromCompany.Any())
                await _context.SaveChangesAsync();

            ViewData["PlainPage"] = true;
            ViewBag.Site = site;
            ViewBag.Requirements = requirements;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ContractorScheme")]
        public async Task<IActionResult> SendRequirement(int siteId, string message, IFormFile? attachment)
        {
            if (!await CanAccessSiteAsync(siteId))
                return Forbid();

            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            string? attachmentPath = null;
            string? attachmentFileName = null;
            if (attachment != null && attachment.Length > 0)
            {
                var result = await _fileUpload.SaveFileUnrestrictedAsync(attachment, $"sites/{siteId}/requirements");
                if (result.Success)
                {
                    attachmentPath = result.FilePath;
                    attachmentFileName = attachment.FileName;
                }
            }

            _context.SiteRequirements.Add(new SiteRequirement
            {
                SiteId = siteId,
                Message = message,
                AttachmentPath = attachmentPath,
                AttachmentFileName = attachmentFileName,
                IsFromContractor = true,
                SentByContractorId = CurrentContractorId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إرسال المتطلب للشركة بنجاح";
            return RedirectToAction("SiteDetails", new { siteId });
        }
    }
}