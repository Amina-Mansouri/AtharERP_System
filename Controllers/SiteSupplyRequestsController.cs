using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteSupplyRequestsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly PermissionService _permissionService;
        private readonly FileUploadService _fileUpload;
        private readonly FinancePdfService _pdfExport;

        public SiteSupplyRequestsController(AppDbContext context, AuditService audit, PermissionService permissionService, FileUploadService fileUpload, FinancePdfService pdfExport)
        {
            _context = context;
            _audit = audit;
            _permissionService = permissionService;
            _fileUpload = fileUpload;
            _pdfExport = pdfExport;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("Supply.View")]
        public async Task<IActionResult> Index(int siteId, int? materialCategoryId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            var requests = await _context.SiteSupplyRequests
                .Include(r => r.RequestedBy)
                .Include(r => r.RequestedByContractor)
                .Include(r => r.Items).ThenInclude(i => i.Vendor)
                .Include(r => r.Items).ThenInclude(i => i.MaterialItem)
                .Where(r => r.SiteId == siteId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            ViewBag.Site = site;
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync();
            ViewBag.MaterialCategories = await _context.MaterialCategories.Where(c => c.IsActive).OrderBy(c => c.NameAr).ToListAsync();
            ViewBag.MaterialItems = await _context.MaterialItems.Where(i => i.IsActive).Include(i => i.MaterialCategory).OrderBy(i => i.Name).ToListAsync();
            ViewBag.MaterialCategoryId = materialCategoryId;
            return View(requests);
        }

        [RequirePermission("Supply.Approve")]
        public async Task<IActionResult> AllRequests(int? projectId, int? subProjectId, int? materialCategoryId)
        {
            var canViewAll = await _permissionService.HasPermissionAsync(User, "Projects.ViewAll");
            var myProjectIds = await _context.ProjectTeamMembers
                .Where(tm => tm.UserId == CurrentUserId)
                .Select(tm => tm.ProjectId)
                .ToListAsync();

            var accessibleProjectsQuery = _context.Projects.AsQueryable();
            if (!canViewAll)
                accessibleProjectsQuery = accessibleProjectsQuery.Where(p => p.CreatedById == CurrentUserId || myProjectIds.Contains(p.Id));

            var accessibleProjects = await accessibleProjectsQuery.OrderBy(p => p.Name).ToListAsync();
            ViewBag.Projects = accessibleProjects.Where(p => p.Scope == ProjectScope.Main).ToList();
            ViewBag.SubProjects = projectId.HasValue
                ? await _context.Projects.Where(p => p.ParentProjectId == projectId.Value).OrderBy(p => p.Name).ToListAsync()
                : new List<Project>();
            ViewBag.MainProjectId = projectId;
            ViewBag.SubProjectId = subProjectId;
            ViewBag.MaterialCategories = await _context.MaterialCategories.Where(c => c.IsActive).OrderBy(c => c.NameAr).ToListAsync();
            ViewBag.MaterialCategoryId = materialCategoryId;

            var effectiveProjectId = subProjectId ?? projectId;

            var query = _context.SiteSupplyRequests
                .Include(r => r.Site).ThenInclude(s => s.Project)
                .Include(r => r.RequestedBy)
                .Include(r => r.RequestedByContractor)
                .Include(r => r.Items).ThenInclude(i => i.Vendor)
                .Include(r => r.Items).ThenInclude(i => i.MaterialItem)
                .AsQueryable();

            if (!canViewAll)
                query = query.Where(r => r.Project.CreatedById == CurrentUserId || myProjectIds.Contains(r.ProjectId));

            if (effectiveProjectId.HasValue)
                query = query.Where(r => r.ProjectId == effectiveProjectId.Value);

            var requests = await query.OrderByDescending(r => r.RequestDate).ToListAsync();

            if (materialCategoryId.HasValue)
                requests = requests.Where(r => r.Items.Any(i => i.MaterialItem != null && i.MaterialItem.MaterialCategoryId == materialCategoryId.Value)).ToList();

            return View(requests);
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportSelectedPdf(List<int> itemIds)
        {
            if (itemIds == null || !itemIds.Any())
            {
                TempData["Error"] = "اختاري سطراً واحداً على الأقل للتصدير";
                return RedirectToAction("AllRequests");
            }

            var items = await _context.SiteSupplyRequestItems
                .Include(i => i.SiteSupplyRequest).ThenInclude(r => r.Site).ThenInclude(s => s.Project)
                .Include(i => i.Vendor)
                .Include(i => i.MaterialItem).ThenInclude(mi => mi!.MaterialCategory)
                .Where(i => itemIds.Contains(i.Id))
                .ToListAsync();

            foreach (var item in items)
            {
                if (!await _permissionService.CanAccessProjectAsync(User, item.SiteSupplyRequest.ProjectId))
                    return Forbid();
            }

            var pdf = _pdfExport.GenerateSupplyRequestsPdf(items);
            return File(pdf, "application/pdf", "طلبات-توريد-مجمّعة.pdf");
        }

        [RequirePermission("Supply.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
      int siteId, string? notes,
      List<int?> materialItemIds, List<decimal> quantities, List<string> units,
      List<int?> vendorIds, List<string?> itemNotes, List<IFormFile> photos)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            if (site.Status == SiteStatus.Completed)
            {
                TempData["Error"] = "لا يمكن إضافة طلب توريد لموقع مكتمل";
                return RedirectToAction("Index", new { siteId });
            }

            var itemCount = materialItemIds?.Count ?? 0;
            if (itemCount == 0 || !materialItemIds!.Any(m => m.HasValue))
            {
                TempData["Error"] = "يجب اختيار صنف واحد على الأقل";
                return RedirectToAction("Index", new { siteId });
            }

            var request = new SiteSupplyRequest
            {
                SiteId = siteId,
                ProjectId = site.ProjectId,
                Notes = notes,
                Status = SiteSupplyStatus.Pending,
                RequestDate = DateTime.UtcNow,
                RequestedById = CurrentUserId
            };
            _context.SiteSupplyRequests.Add(request);
            await _context.SaveChangesAsync();

            for (int i = 0; i < itemCount; i++)
            {
                if (!materialItemIds![i].HasValue)
                    continue;

                var materialItem = await _context.MaterialItems.FindAsync(materialItemIds[i]!.Value);
                if (materialItem == null)
                    continue;

                string? photoPath = null;
                if (photos != null && i < photos.Count && photos[i] != null && photos[i].Length > 0)
                {
                    var result = await _fileUpload.SaveFileAsync(photos[i], $"sites/{siteId}/supply-requests/{request.Id}");
                    if (result.Success)
                        photoPath = result.FilePath;
                }

                _context.SiteSupplyRequestItems.Add(new SiteSupplyRequestItem
                {
                    SiteSupplyRequestId = request.Id,
                    MaterialItemId = materialItem.Id,
                    MaterialName = materialItem.Name,
                    Quantity = i < quantities.Count ? quantities[i] : 0,
                    Unit = i < units.Count ? units[i] : "",
                    VendorId = i < vendorIds.Count ? vendorIds[i] : null,
                    Notes = i < itemNotes.Count ? itemNotes[i] : null,
                    PhotoPath = photoPath
                });
            }
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Create", nameof(SiteSupplyRequest), request.Id.ToString(), "إضافة طلب توريد");

            TempData["Success"] = "تم إرسال طلب التوريد بنجاح";
            return RedirectToAction("Index", new { siteId });
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, SiteSupplyStatus status)
        {
            var request = await _context.SiteSupplyRequests.Include(r => r.Site).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, request.Site.ProjectId))
                return Forbid();

            request.Status = status;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(SiteSupplyRequest), id.ToString(), $"تحديث حالة طلب توريد إلى: {status}");

            TempData["Success"] = "تم تحديث حالة طلب التوريد";
            return RedirectToAction("Index", new { siteId = request.SiteId });
        }

        [RequirePermission("Supply.Approve")]
        [HttpGet]
        public async Task<IActionResult> RegisterExpense(int id)
        {
            var request = await _context.SiteSupplyRequests.Include(r => r.Site).Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, request.Site.ProjectId))
                return Forbid();

            if (request.Status != SiteSupplyStatus.Approved)
            {
                TempData["Error"] = "لا يمكن تسجيل صرف لطلب لم تتم الموافقة عليه بعد";
                return RedirectToAction("Index", new { siteId = request.SiteId });
            }

            var category = await _context.ExpenseCategories.FirstOrDefaultAsync(c => c.NameAr == "طلبات التوريد" && c.Scope == ExpenseCategoryScope.Project);
            if (category == null)
            {
                category = new ExpenseCategory { NameAr = "طلبات التوريد", Scope = ExpenseCategoryScope.Project, IsActive = true };
                _context.ExpenseCategories.Add(category);
                await _context.SaveChangesAsync();
            }

            var itemsSummary = string.Join(" ، ", request.Items.Select(i => $"{i.MaterialName} ({i.Quantity:N2} {i.Unit})"));

            return RedirectToAction("Expenses", "Finance", new
            {
                projectId = request.ProjectId,
                prefillSiteId = request.SiteId,
                prefillCategoryId = category.Id,
                prefillDescription = itemsSummary,
                supplyRequestId = request.Id
            });
        }

        [RequirePermission("Supply.Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.SiteSupplyRequests.Include(r => r.Site).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, request.Site.ProjectId))
                return Forbid();

            var siteId = request.SiteId;
            _context.SiteSupplyRequests.Remove(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف طلب التوريد بنجاح";
            return RedirectToAction("Index", new { siteId });
        }
    }
}