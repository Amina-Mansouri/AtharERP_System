using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class FinanceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly ProjectCalculationService _calc;
        private readonly FinancePdfService _pdfExport;
        public FinanceController(AppDbContext context, PermissionService permissionService, ProjectCalculationService calc, FinancePdfService pdfExport)
        {
            _context = context;
            _permissionService = permissionService;
            _calc = calc;
            _pdfExport = pdfExport;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task<List<Project>> GetAccessibleProjectsAsync(int? categoryId = null)
        {
            var canViewAll = await _permissionService.HasPermissionAsync(User, "Projects.ViewAll");
            var myProjectIds = await _context.ProjectTeamMembers
                .Where(tm => tm.UserId == CurrentUserId)
                .Select(tm => tm.ProjectId)
                .ToListAsync();

            var query = _context.Projects.AsQueryable();
            if (!canViewAll)
                query = query.Where(p => p.CreatedById == CurrentUserId || myProjectIds.Contains(p.Id));

            if (categoryId.HasValue)
                query = query.Where(p => p.ProjectCategoryId == categoryId.Value);

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        private async Task<List<ProjectCategory>> GetActiveCategoriesAsync()
        {
            return await _context.ProjectCategories.Where(c => c.IsActive).OrderBy(c => c.Classification).ToListAsync();
        }

        // ============================================
        // جدول التكاليف — داخلي، لحساب مستحقات المهندسات
        // ============================================
        [RequirePermission("Finance.Costs.View")]
        public async Task<IActionResult> CostTable(int? categoryId, int? projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            ViewBag.Categories = await GetActiveCategoriesAsync();
            ViewBag.Projects = await GetAccessibleProjectsAsync(categoryId);
            ViewBag.CategoryId = categoryId;
            ViewBag.ProjectId = projectId;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            if (!projectId.HasValue)
                return View(new List<FinancialRecord>());

            var project = await _context.Projects.FindAsync(projectId.Value);
            if (project == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId.Value))
                return Forbid();

            var query = _context.FinancialRecords
                .Include(r => r.ProjectAssignment)
                .Include(r => r.Engineer)
                .Where(r => r.ProjectId == projectId.Value);

            if (dateFrom.HasValue)
                query = query.Where(r => r.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(r => r.CreatedAt <= dateTo.Value.AddDays(1).AddTicks(-1));

            var records = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            var allProjectTasks = await _context.ProjectTasks
                .Include(t => t.Stage)
                .Include(t => t.Todos).ThenInclude(td => td.DesignProposals)
                .Where(t => t.ProjectId == projectId.Value)
                .ToListAsync();

            decimal EngineerProjectContribution(string engineerId)
            {
                var engineerTasks = allProjectTasks
                    .Where(t => t.Todos.Any(td => td.DesignProposals.Any(dp => dp.PreparedById == engineerId)));

                return engineerTasks.Sum(t => t.Weight * (t.Stage?.Weight ?? 0) / 100);
            }

            ViewBag.Project = project;
            ViewBag.EngineerContributions = records
                .Select(r => r.EngineerId)
                .Distinct()
                .ToDictionary(id => id, id => EngineerProjectContribution(id));

            return View(records);
        }

        [RequirePermission("Finance.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRecordCleared(int id, int projectId)
        {
            var record = await _context.FinancialRecords.FindAsync(id);
            if (record == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId))
                return Forbid();

            if (!record.IsCleared)
            {
                record.IsCleared = true;
                record.ClearedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("CostTable", new { projectId });
        }

        // ============================================
        // جدول البيع النهائي / المطالبة — خارجي، لمطالبات الزبائن
        // ============================================
        [RequirePermission("Finance.Sales.View")]
        public async Task<IActionResult> SaleTable(int? categoryId, int? projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            ViewBag.Categories = await GetActiveCategoriesAsync();
            ViewBag.Projects = await GetAccessibleProjectsAsync(categoryId);
            ViewBag.CategoryId = categoryId;
            ViewBag.ProjectId = projectId;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            if (!projectId.HasValue)
                return View(new List<FinancialClaim>());

            var project = await _context.Projects.FindAsync(projectId.Value);
            if (project == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId.Value))
                return Forbid();

            var query = _context.FinancialClaims
                .Include(c => c.ProjectAssignment)
                .Where(c => c.ProjectId == projectId.Value);

            if (dateFrom.HasValue)
                query = query.Where(c => c.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(c => c.CreatedAt <= dateTo.Value.AddDays(1).AddTicks(-1));

            var claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            ViewBag.Project = project;
            return View(claims);
        }

        [RequirePermission("Finance.Sales.View")]
        public async Task<IActionResult> Claims(int? categoryId, int? projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            ViewBag.Categories = await GetActiveCategoriesAsync();
            ViewBag.Projects = await GetAccessibleProjectsAsync(categoryId);
            ViewBag.CategoryId = categoryId;
            ViewBag.ProjectId = projectId;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            if (!projectId.HasValue)
                return View(new List<FinancialClaim>());

            var project = await _context.Projects.FindAsync(projectId.Value);
            if (project == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId.Value))
                return Forbid();

            var query = _context.FinancialClaims
                .Include(c => c.ProjectAssignment)
                .Where(c => c.ProjectId == projectId.Value);

            if (dateFrom.HasValue)
                query = query.Where(c => c.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(c => c.CreatedAt <= dateTo.Value.AddDays(1).AddTicks(-1));

            var claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            ViewBag.Project = project;
            return View(claims);
        }

        [RequirePermission("Finance.Claims.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkClaimSettled(int id, int projectId, string returnAction = "SaleTable")
        
            {
            var claim = await _context.FinancialClaims.FindAsync(id);
            if (claim == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId))
                return Forbid();

            if (!claim.IsClientSettled)
            {
                claim.IsClientSettled = true;
                claim.ClientSettledAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(returnAction == "Claims" ? "Claims" : "SaleTable", new { projectId });
        }

        [RequirePermission("Finance.Claims.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClaimPercentages(int id, int projectId, decimal? percent1, decimal? percent2)
        {
            var claim = await _context.FinancialClaims.FindAsync(id);
            if (claim == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId))
                return Forbid();

            claim.SaleMarkupPercent1 = percent1;
            claim.SaleMarkupPercent2 = percent2;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ النسبتين بنجاح";
            return RedirectToAction("SaleTable", new { projectId });
        }
        // ============================================
        // المصروفات — عامة أو مرتبطة بموقع محدد
        // ============================================
        [RequirePermission("Finance.Costs.View")]
        public async Task<IActionResult> Expenses(int? categoryId, int? projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            ViewBag.Categories = await GetActiveCategoriesAsync();
            ViewBag.Projects = await GetAccessibleProjectsAsync(categoryId);
            ViewBag.CategoryId = categoryId;
            ViewBag.ProjectId = projectId;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            if (!projectId.HasValue)
                return View(new List<ProjectExpense>());

            var project = await _context.Projects.FindAsync(projectId.Value);
            if (project == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId.Value))
                return Forbid();

            var query = _context.ProjectExpenses
                .Include(e => e.ExpenseCategory)
                .Include(e => e.Site)
                .Where(e => e.ProjectId == projectId.Value);

            if (dateFrom.HasValue)
                query = query.Where(e => e.Date >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(e => e.Date <= dateTo.Value);

            var expenses = await query.OrderByDescending(e => e.Date).ToListAsync();

            ViewBag.Project = project;
            ViewBag.ExpenseCategories = await _context.ExpenseCategories.Where(c => c.IsActive && c.Scope == ExpenseCategoryScope.Project).OrderBy(c => c.NameAr).ToListAsync();
            ViewBag.Sites = await _context.Sites.Where(s => s.ProjectId == projectId.Value).OrderBy(s => s.Name).ToListAsync();

            return View(expenses);
        }

        [RequirePermission("Finance.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExpense([Bind("ProjectId,ExpenseCategoryId,SiteId,Amount,Date,Description")] ProjectExpense model)
        {
            if (!model.ProjectId.HasValue || !await _permissionService.CanAccessProjectAsync(User, model.ProjectId.Value))
                return Forbid();

            model.CreatedAt = DateTime.UtcNow;
            _context.ProjectExpenses.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تسجيل المصروف بنجاح";
            return RedirectToAction("Expenses", new { projectId = model.ProjectId });
        }

        [RequirePermission("Finance.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExpense(int id, int projectId)
        {
            var expense = await _context.ProjectExpenses.FindAsync(id);
            if (expense == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId))
                return Forbid();

            _context.ProjectExpenses.Remove(expense);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف المصروف بنجاح";
            return RedirectToAction("Expenses", new { projectId });
        }

        // ============================================
        // الأرباح والخسائر
        // ============================================
        [RequirePermission("Finance.Reports")]
        public async Task<IActionResult> Profit(int? categoryId, int? projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            ViewBag.Categories = await GetActiveCategoriesAsync();
            ViewBag.Projects = await GetAccessibleProjectsAsync(categoryId);
            ViewBag.CategoryId = categoryId;
            ViewBag.ProjectId = projectId;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            if (!projectId.HasValue)
                return View();

            var project = await _context.Projects.FindAsync(projectId.Value);
            if (project == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId.Value))
                return Forbid();

            ViewBag.Project = project;
            var result = await _calc.CalculateProjectNetProfitAsync(projectId.Value, dateFrom, dateTo);
            return View(result);
        }

        [RequirePermission("Finance.Sales.View")]
        public async Task<IActionResult> ExportSaleTablePdf(int projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();
            if (!await _permissionService.CanAccessProjectAsync(User, projectId)) return Forbid();

            var query = _context.FinancialClaims.Include(c => c.ProjectAssignment).Where(c => c.ProjectId == projectId);
            if (dateFrom.HasValue) query = query.Where(c => c.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(c => c.CreatedAt <= dateTo.Value.AddDays(1).AddTicks(-1));
            var claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            var pdf = _pdfExport.GenerateClaimsReport("جدول البيع النهائي", new() { (project, claims) }, includePercentageColumns: true);
            return File(pdf, "application/pdf", $"جدول-البيع-{project.Code}.pdf");
        }

        [RequirePermission("Finance.Sales.View")]
        public async Task<IActionResult> ExportClaimsPdf(int projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();
            if (!await _permissionService.CanAccessProjectAsync(User, projectId)) return Forbid();

            var query = _context.FinancialClaims.Include(c => c.ProjectAssignment).Where(c => c.ProjectId == projectId);
            if (dateFrom.HasValue) query = query.Where(c => c.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(c => c.CreatedAt <= dateTo.Value.AddDays(1).AddTicks(-1));
            var claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            var pdf = _pdfExport.GenerateClaimsReport("المطالبة", new() { (project, claims) }, includePercentageColumns: false);
            return File(pdf, "application/pdf", $"المطالبة-{project.Code}.pdf");
        }

        [RequirePermission("Finance.Sales.View")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportSaleTableBatchPdf(List<int> projectIds, DateTime? dateFrom, DateTime? dateTo)
        {
            if (projectIds == null || !projectIds.Any())
            {
                TempData["Error"] = "اختاري مشروعاً واحداً على الأقل للتصدير الجماعي";
                return RedirectToAction("SaleTable");
            }

            var sections = new List<(Project, List<FinancialClaim>)>();
            foreach (var pid in projectIds)
            {
                if (!await _permissionService.CanAccessProjectAsync(User, pid)) continue;
                var project = await _context.Projects.FindAsync(pid);
                if (project == null) continue;

                var query = _context.FinancialClaims.Include(c => c.ProjectAssignment).Where(c => c.ProjectId == pid);
                if (dateFrom.HasValue) query = query.Where(c => c.CreatedAt >= dateFrom.Value);
                if (dateTo.HasValue) query = query.Where(c => c.CreatedAt <= dateTo.Value.AddDays(1).AddTicks(-1));
                var claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
                sections.Add((project, claims));
            }

            var pdf = _pdfExport.GenerateClaimsReport("جدول البيع النهائي — تصدير جماعي", sections, includePercentageColumns: true);
            return File(pdf, "application/pdf", $"جدول-البيع-دفعة-{DateTime.UtcNow:yyyyMMdd}.pdf");
        }

        [RequirePermission("Finance.Sales.View")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportClaimsBatchPdf(List<int> projectIds, DateTime? dateFrom, DateTime? dateTo)
        {
            if (projectIds == null || !projectIds.Any())
            {
                TempData["Error"] = "اختاري مشروعاً واحداً على الأقل للتصدير الجماعي";
                return RedirectToAction("Claims");
            }

            var sections = new List<(Project, List<FinancialClaim>)>();
            foreach (var pid in projectIds)
            {
                if (!await _permissionService.CanAccessProjectAsync(User, pid)) continue;
                var project = await _context.Projects.FindAsync(pid);
                if (project == null) continue;

                var query = _context.FinancialClaims.Include(c => c.ProjectAssignment).Where(c => c.ProjectId == pid);
                if (dateFrom.HasValue) query = query.Where(c => c.CreatedAt >= dateFrom.Value);
                if (dateTo.HasValue) query = query.Where(c => c.CreatedAt <= dateTo.Value.AddDays(1).AddTicks(-1));
                var claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
                sections.Add((project, claims));
            }

            var pdf = _pdfExport.GenerateClaimsReport("المطالبة — تصدير جماعي", sections, includePercentageColumns: false);
            return File(pdf, "application/pdf", $"المطالبة-دفعة-{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
    }
}