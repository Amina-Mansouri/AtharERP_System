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
        private readonly SiteCalculationService _siteCalc;
        private readonly FileUploadService _fileUpload;

        public ContractorPortalController(AppDbContext context, SiteCalculationService siteCalc, FileUploadService fileUpload)
        {
            _context = context;
            _siteCalc = siteCalc;
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

            var dailyReports = await _context.SiteDailyReports
     .Include(r => r.Photos)
     .Where(r => r.SiteId == siteId && r.CreatedByContractorId == CurrentContractorId)
     .OrderByDescending(r => r.ReportDate)
     .Take(10)
     .ToListAsync();

            var qualityChecks = await _context.SiteQualityChecks
                .Where(q => q.SiteId == siteId && q.CheckedByContractorId == CurrentContractorId)
                .OrderByDescending(q => q.CheckDate)
                .Take(10)
                .ToListAsync();

            var safetyChecks = await _context.SiteSafetyChecks
                .Where(s => s.SiteId == siteId && s.CheckedByContractorId == CurrentContractorId)
                .OrderByDescending(s => s.CheckDate)
                .Take(10)
                .ToListAsync();

            var supplyRequests = await _context.SiteSupplyRequests
                .Where(r => r.SiteId == siteId && r.RequestedByContractorId == CurrentContractorId)
                .OrderByDescending(r => r.RequestDate)
                .Take(10)
                .ToListAsync();

            ViewData["PlainPage"] = true;
            ViewBag.Site = site;
            ViewBag.DailyReports = dailyReports;
            ViewBag.QualityChecks = qualityChecks;
            ViewBag.SafetyChecks = safetyChecks;
            ViewBag.SupplyRequests = supplyRequests;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ContractorScheme")]
        public async Task<IActionResult> UpdateOperationDates(int id, DateTime? actualStartDate, DateTime? actualEndDate)
        {
            var op = await _context.SiteOperations.FirstOrDefaultAsync(o => o.Id == id);
            if (op == null)
                return NotFound();

            if (!await CanAccessSiteAsync(op.SiteId))
                return Forbid();

            if (op.Status != OperationStatus.OnHold)
            {
                op.ActualStartDate = actualStartDate;
                op.ActualEndDate = actualEndDate;
                SiteCalculationService.ApplyAutomaticOperationStatus(op);
            }

            await _context.SaveChangesAsync();
            await _siteCalc.ApplyAutomaticSiteStatusAsync(op.SiteId);

            TempData["Success"] = "تم تحديث مرحلة العمل بنجاح";
            return RedirectToAction("SiteDetails", new { siteId = op.SiteId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ContractorScheme")]
        public async Task<IActionResult> CreateDailyReport(
            int siteId, DateTime reportDate, string? weather, int workersCount, string? workCompleted,
            string? issues, string? materialsUsed, string? equipmentUsed, string? visits, string? notes,
            List<IFormFile>? photos)
        {
            if (!await CanAccessSiteAsync(siteId))
                return Forbid();

            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (site.Status == SiteStatus.Completed)
            {
                TempData["Error"] = "لا يمكن إضافة تقرير لموقع مكتمل";
                return RedirectToAction("SiteDetails", new { siteId });
            }

            var report = new SiteDailyReport
            {
                SiteId = siteId,
                ReportDate = reportDate,
                Weather = weather,
                WorkersCount = workersCount,
                WorkCompleted = workCompleted,
                Issues = issues,
                MaterialsUsed = materialsUsed,
                EquipmentUsed = equipmentUsed,
                Visits = visits,
                Notes = notes,
                CreatedByContractorId = CurrentContractorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SiteDailyReports.Add(report);
            await _context.SaveChangesAsync();

            if (photos != null)
            {
                foreach (var photo in photos.Where(p => p.Length > 0))
                {
                    var result = await _fileUpload.SaveFileAsync(photo, $"sites/{siteId}/daily-reports/{report.Id}");
                    if (result.Success)
                    {
                        _context.SiteDailyReportPhotos.Add(new SiteDailyReportPhoto
                        {
                            DailyReportId = report.Id,
                            FilePath = result.FilePath!,
                            UploadedAt = DateTime.UtcNow
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "تمت إضافة التقرير اليومي بنجاح";
            return RedirectToAction("SiteDetails", new { siteId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ContractorScheme")]
        public async Task<IActionResult> CreateQualityCheck(int siteId, SiteQualityType qualityType, string checkType, string? description)
        {
            if (!await CanAccessSiteAsync(siteId))
                return Forbid();

            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (site.Status == SiteStatus.Completed)
            {
                TempData["Error"] = "لا يمكن إضافة فحص لموقع مكتمل";
                return RedirectToAction("SiteDetails", new { siteId });
            }

            _context.SiteQualityChecks.Add(new SiteQualityCheck
            {
                SiteId = siteId,
                QualityType = qualityType,
                CheckType = checkType,
                Description = description,
                Result = QualityCheckResult.Pending,
                CheckDate = DateTime.UtcNow,
                CheckedByContractorId = CurrentContractorId,
                IsApproved = false
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة فحص الجودة بنجاح";
            return RedirectToAction("SiteDetails", new { siteId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ContractorScheme")]
        public async Task<IActionResult> CreateSafetyCheck(int siteId, string checkType, string? description)
        {
            if (!await CanAccessSiteAsync(siteId))
                return Forbid();

            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (site.Status == SiteStatus.Completed)
            {
                TempData["Error"] = "لا يمكن إضافة فحص لموقع مكتمل";
                return RedirectToAction("SiteDetails", new { siteId });
            }

            _context.SiteSafetyChecks.Add(new SiteSafetyCheck
            {
                SiteId = siteId,
                CheckType = checkType,
                Description = description,
                Result = SafetyResult.Safe,
                CheckDate = DateTime.UtcNow,
                CheckedByContractorId = CurrentContractorId,
                IsApproved = false
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة فحص السلامة بنجاح";
            return RedirectToAction("SiteDetails", new { siteId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ContractorScheme")]
        public async Task<IActionResult> CreateSupplyRequest(int siteId, string materialName, string? dimensions, decimal quantity, string unit, string? notes)
        {
            if (!await CanAccessSiteAsync(siteId))
                return Forbid();

            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (site.Status == SiteStatus.Completed)
            {
                TempData["Error"] = "لا يمكن إضافة طلب توريد لموقع مكتمل";
                return RedirectToAction("SiteDetails", new { siteId });
            }

            _context.SiteSupplyRequests.Add(new SiteSupplyRequest
            {
                SiteId = siteId,
                ProjectId = site.ProjectId,
                MaterialName = materialName,
                Dimensions = dimensions,
                Quantity = quantity,
                Unit = unit,
                Notes = notes,
                Status = SiteSupplyStatus.Pending,
                RequestDate = DateTime.UtcNow,
                RequestedByContractorId = CurrentContractorId
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إرسال طلب التوريد بنجاح";
            return RedirectToAction("SiteDetails", new { siteId });
        }
    }
}