using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteContractorsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly PermissionService _permissionService;

        public SiteContractorsController(AppDbContext context, AuditService audit, PermissionService permissionService)
        {
            _context = context;
            _audit = audit;
            _permissionService = permissionService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة مقاولي موقع معيّن (أو كل المقاولين)
        // ============================================
        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Index(int? siteId)
        {
            Site? site = null;
            var query = _context.SiteContractors
                .Include(c => c.Contractor)
                .Include(c => c.Site).ThenInclude(s => s.Project)
                .AsQueryable();

            if (siteId.HasValue)
            {
                site = await _context.Sites.FindAsync(siteId.Value);
                if (site == null)
                    return NotFound();

                if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                    return Forbid();

                query = query.Where(c => c.SiteId == siteId.Value);
            }
            else if (!await _permissionService.HasPermissionAsync(User, "Projects.ViewAll"))
            {
                var myProjectIds = await _context.ProjectTeamMembers
                    .Where(tm => tm.UserId == CurrentUserId)
                    .Select(tm => tm.ProjectId)
                    .ToListAsync();

                query = query.Where(c => c.Site.Project.CreatedById == CurrentUserId || myProjectIds.Contains(c.Site.ProjectId));
            }

            var contractors = await query
                .OrderByDescending(c => c.Status == ContractorStatus.Active)
                .ThenBy(c => c.Contractor.Name)
                .ToListAsync();

            if (site != null)
            {
                ViewBag.AllContractors = await _context.Contractors
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }

            ViewBag.Site = site;
            return View(contractors);
        }

        // ============================================
        // ربط مقاول بموقع (مقاول موجود، أو إنشاء مقاول جديد وربطه)
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int siteId, int? contractorId, string? newName, string? newCompanyName, string? newPhone,
            string? newEmail, string? newPassword, string? specialty, DateTime? startDate, DateTime? endDate)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            async Task<IActionResult> FailAsync(string error)
            {
                ViewBag.Error = error;
                ViewBag.Site = site;
                ViewBag.AllContractors = await _context.Contractors.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
                ViewBag.PostedContractorId = contractorId;
                ViewBag.PostedNewName = newName;
                ViewBag.PostedNewCompanyName = newCompanyName;
                ViewBag.PostedNewPhone = newPhone;
                ViewBag.PostedNewEmail = newEmail;
                ViewBag.PostedNewPassword = newPassword;
                ViewBag.PostedSpecialty = specialty;
                ViewBag.PostedStartDate = startDate;
                ViewBag.PostedEndDate = endDate;

                var listData = await _context.SiteContractors
                    .Include(c => c.Contractor)
                    .Include(c => c.Site).ThenInclude(s => s.Project)
                    .Where(c => c.SiteId == siteId)
                    .OrderByDescending(c => c.Status == ContractorStatus.Active)
                    .ThenBy(c => c.Contractor.Name)
                    .ToListAsync();
                return View("Index", listData);
            }

            Contractor? contractor;

            if (contractorId.HasValue)
            {
                contractor = await _context.Contractors.FindAsync(contractorId.Value);
                if (contractor == null)
                    return await FailAsync("المقاول المحدد غير موجود");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(newName) || string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(newPassword))
                    return await FailAsync("لإنشاء مقاول جديد: الاسم والبريد الإلكتروني وكلمة المرور مطلوبة");

                if (await _context.Contractors.AnyAsync(c => c.Email == newEmail))
                    return await FailAsync("البريد الإلكتروني مستخدم بالفعل لمقاول آخر");

                contractor = new Contractor
                {
                    Name = newName,
                    CompanyName = newCompanyName,
                    Phone = newPhone,
                    Email = newEmail,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                contractor.PasswordHash = new PasswordHasher<Contractor>().HashPassword(contractor, newPassword);

                _context.Contractors.Add(contractor);
                await _context.SaveChangesAsync();

                await _audit.LogAsync(CurrentUserId, "Create", nameof(Contractor), contractor.Id.ToString(), $"إنشاء حساب مقاول: {contractor.Name} ({contractor.Email})");
            }

            var alreadyLinked = await _context.SiteContractors.AnyAsync(sc => sc.SiteId == siteId && sc.ContractorId == contractor.Id);
            if (alreadyLinked)
                return await FailAsync("هذا المقاول مرتبط بهذا الموقع بالفعل");

            _context.SiteContractors.Add(new SiteContractor
            {
                SiteId = siteId,
                ContractorId = contractor.Id,
                Specialty = specialty,
                StartDate = startDate,
                EndDate = endDate,
                Status = ContractorStatus.Active
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم ربط المقاول {contractor.Name} بالموقع بنجاح";
            return RedirectToAction("Index", new { siteId });
        }

        // ============================================
        // تعديل بيانات ارتباط المقاول بالموقع (التخصص/التواريخ/الحالة)
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var contractor = await _context.SiteContractors.Include(c => c.Site).Include(c => c.Contractor).FirstOrDefaultAsync(c => c.Id == id);
            if (contractor == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, contractor.Site.ProjectId))
                return Forbid();

            return View(contractor);
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Specialty,StartDate,EndDate,Status,Notes")] SiteContractor model)
        {
            var contractor = await _context.SiteContractors.Include(c => c.Site).Include(c => c.Contractor).FirstOrDefaultAsync(c => c.Id == id);
            if (contractor == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, contractor.Site.ProjectId))
                return Forbid();

            if (!ModelState.IsValid)
                return View(contractor);

            contractor.Specialty = model.Specialty;
            contractor.StartDate = model.StartDate;
            contractor.EndDate = model.EndDate;
            contractor.Status = model.Status;
            contractor.Notes = model.Notes;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم تحديث بيانات ارتباط المقاول {contractor.Contractor.Name} بنجاح";
            return RedirectToAction("Index", new { siteId = contractor.SiteId });
        }

        // ============================================
        // إلغاء ربط مقاول بموقع (لا يحذف حساب المقاول نفسه)
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var contractor = await _context.SiteContractors.Include(c => c.Site).Include(c => c.Contractor).FirstOrDefaultAsync(c => c.Id == id);
            if (contractor == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, contractor.Site.ProjectId))
                return Forbid();

            var siteId = contractor.SiteId;
            var name = contractor.Contractor.Name;

            _context.SiteContractors.Remove(contractor);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(SiteContractor), id.ToString(), $"إلغاء ربط مقاول: {name}");

            TempData["Success"] = "تم إلغاء ربط المقاول بالموقع بنجاح";
            return RedirectToAction("Index", new { siteId });
        }
    }
}