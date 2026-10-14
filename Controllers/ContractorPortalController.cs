using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
        private readonly SiteCorrespondencePdfService _pdfService;
        private readonly NotificationService _notify;
        private readonly IWebHostEnvironment _environment;

        public ContractorPortalController(AppDbContext context, FileUploadService fileUpload,
            SiteCorrespondencePdfService pdfService, NotificationService notify, IWebHostEnvironment environment)
        {
            _context = context;
            _fileUpload = fileUpload;
            _pdfService = pdfService;
            _notify = notify;
            _environment = environment;
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
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == siteId);
            if (site == null)
                return NotFound();

            var items = await _context.SiteCorrespondences
                .Include(r => r.SentBy)
                .Include(r => r.SentByContractor)
                .Where(r => r.SiteId == siteId)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            var unreadFromCompany = items.Where(r => !r.IsFromContractor && !r.IsRead).ToList();
            foreach (var r in unreadFromCompany)
                r.IsRead = true;
            if (unreadFromCompany.Any())
                await _context.SaveChangesAsync();

            ViewData["PlainPage"] = true;
            ViewBag.Site = site;
            ViewBag.Correspondences = items;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ContractorScheme")]
        public async Task<IActionResult> SendCorrespondence(int siteId, int? parentCorrespondenceId, CorrespondencePriority priority,
            DateTime? requiredResponseDate, string detailedQuery, IFormFile? attachment)
        {
            if (!await CanAccessSiteAsync(siteId))
                return Forbid();

            var site = await _context.Sites.Include(s => s.Project).FirstOrDefaultAsync(s => s.Id == siteId);
            if (site == null)
                return NotFound();

            var contractor = await _context.Contractors.FindAsync(CurrentContractorId);

            string? attachmentPath = null;
            string? attachmentFileName = null;
            if (attachment != null && attachment.Length > 0)
            {
                var result = await _fileUpload.SaveFileUnrestrictedAsync(attachment, $"sites/{siteId}/correspondences");
                if (result.Success)
                {
                    attachmentPath = result.FilePath;
                    attachmentFileName = attachment.FileName;
                }
            }

            var nextRequestNumber = (await _context.SiteCorrespondences.MaxAsync(r => (int?)r.RequestNumber) ?? 0) + 1;

            var item = new SiteCorrespondence
            {
                SiteId = siteId,
                RequestNumber = nextRequestNumber,
                Priority = priority,
                RequiredResponseDate = requiredResponseDate,
                DetailedQuery = detailedQuery,
                AttachmentPath = attachmentPath,
                AttachmentFileName = attachmentFileName,
                IsFromContractor = true,
                SubmittedByName = contractor?.Name ?? "-",
                SubmittedByPosition = contractor?.CompanyName,
                SentByContractorId = CurrentContractorId,
                ParentCorrespondenceId = parentCorrespondenceId,
                Status = CorrespondenceStatus.Pending,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var pdfBytes = _pdfService.Generate(item, site);
            var pdfResult = await _fileUpload.SaveGeneratedFileAsync(pdfBytes, $"correspondences/site-{siteId}", ".pdf");
            if (pdfResult.Success)
                item.PdfFilePath = pdfResult.FilePath;

            _context.SiteCorrespondences.Add(item);

            if (parentCorrespondenceId.HasValue)
            {
                var parent = await _context.SiteCorrespondences.FindAsync(parentCorrespondenceId.Value);
                if (parent != null)
                    parent.Status = CorrespondenceStatus.Resolved;
            }

            await _context.SaveChangesAsync();

            var managerIds = await _context.SiteManagers.Where(m => m.SiteId == siteId).Select(m => m.UserId).ToListAsync();
            if (managerIds.Any())
            {
                await _notify.NotifyManyAsync(managerIds, $"مراسلة جديدة من المقاول على الموقع {site.Name}",
                    NotificationEventType.SiteCorrespondenceReceived, $"/SiteCorrespondences/Index?siteId={siteId}");
            }

            TempData["Success"] = "تم إرسال المراسلة للشركة بنجاح";
            return RedirectToAction("SiteDetails", new { siteId });
        }

        [Authorize(AuthenticationSchemes = "ContractorScheme")]
        public async Task<IActionResult> DownloadCorrespondence(int id)
        {
            var item = await _context.SiteCorrespondences.FirstOrDefaultAsync(r => r.Id == id);
            if (item == null || string.IsNullOrEmpty(item.PdfFilePath))
                return NotFound();

            if (!await CanAccessSiteAsync(item.SiteId))
                return Forbid();

            var pdfFullPath = Path.Combine(_environment.WebRootPath, item.PdfFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(pdfFullPath))
                return NotFound();

            return PhysicalFile(pdfFullPath, "application/pdf", $"مراسلة-{item.RequestNumber:D4}.pdf");
        }
    }
}