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

        private async Task<List<Project>> GetAccessibleMainProjectsAsync(int? categoryId = null)
        {
            var all = await GetAccessibleProjectsAsync(categoryId);
            return all.Where(p => p.Scope == ProjectScope.Main).ToList();
        }

        private async Task<List<Project>> GetSubProjectsAsync(int? parentProjectId)
        {
            if (!parentProjectId.HasValue)
                return new List<Project>();
            return await _context.Projects.Where(p => p.ParentProjectId == parentProjectId.Value).OrderBy(p => p.Name).ToListAsync();
        }
        private async Task<List<ProjectCategory>> GetActiveCategoriesAsync()
        {
            return await _context.ProjectCategories.Where(c => c.IsActive).OrderBy(c => c.Classification).ToListAsync();
        }

        // ============================================
        // جدول التكاليف — داخلي، لحساب مستحقات المهندسات
        // ============================================
        [RequirePermission("Finance.Costs.View")]
        public async Task<IActionResult> CostTable(int? categoryId, List<int>? projectIds, List<int>? subProjectIds, DateTime? dateFrom, DateTime? dateTo)
        {
            projectIds ??= new List<int>();
            subProjectIds ??= new List<int>();
            var singleProjectId = projectIds.Count == 1 ? projectIds[0] : (int?)null;
            var accessibleProjects = await GetAccessibleProjectsAsync(categoryId);

            ViewBag.Categories = await GetActiveCategoriesAsync();
            ViewBag.Projects = accessibleProjects.Where(p => p.Scope == ProjectScope.Main).ToList();
            ViewBag.CategoryId = categoryId;
            ViewBag.ProjectIds = projectIds;
            ViewBag.ProjectId = singleProjectId;
            ViewBag.MainProjectId = singleProjectId;
            ViewBag.SubProjectIds = subProjectIds;
            ViewBag.SubProjects = singleProjectId.HasValue ? await GetSubProjectsAsync(singleProjectId) : new List<Project>();
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            List<int> targetProjectIds;
            Project? project = null;

            if (singleProjectId.HasValue)
            {
                project = await _context.Projects.FindAsync(singleProjectId.Value);
                if (project == null)
                    return NotFound();

                if (!await _permissionService.CanAccessProjectAsync(User, singleProjectId.Value))
                    return Forbid();

                if (subProjectIds.Any())
                {
                    foreach (var spid in subProjectIds)
                    {
                        if (!await _permissionService.CanAccessProjectAsync(User, spid))
                            return Forbid();
                    }
                    targetProjectIds = subProjectIds;
                }
                else
                {
                    var childIds = await _context.Projects.Where(p => p.ParentProjectId == singleProjectId.Value).Select(p => p.Id).ToListAsync();
                    targetProjectIds = new List<int> { singleProjectId.Value };
                    targetProjectIds.AddRange(childIds);
                }
            }
            else if (projectIds.Any())
            {
                foreach (var pid in projectIds)
                {
                    if (!await _permissionService.CanAccessProjectAsync(User, pid))
                        return Forbid();
                }
                targetProjectIds = projectIds;
            }
            else
            {
                targetProjectIds = accessibleProjects.Select(p => p.Id).ToList();
            }

            ViewBag.Project = project;

            var query = _context.FinancialRecords
                .Include(r => r.ProjectAssignment)
                .Include(r => r.Engineer)
                .Include(r => r.Project)
                .Where(r => targetProjectIds.Contains(r.ProjectId));

            if (dateFrom.HasValue)
                query = query.Where(r => r.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(r => r.CreatedAt <= dateTo.Value.AddDays(1).AddTicks(-1));

            var records = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            var allProjectTasks = await _context.ProjectTasks
                .Include(t => t.Stage)
                .Include(t => t.Todos).ThenInclude(td => td.DesignProposals)
                .Where(t => targetProjectIds.Contains(t.ProjectId))
                .ToListAsync();

            decimal EngineerProjectContribution(string engineerId)
            {
                var engineerTasks = allProjectTasks
                    .Where(t => t.Todos.Any(td => td.DesignProposals.Any(dp => dp.PreparedById == engineerId)));

                return engineerTasks.Sum(t => t.Weight * (t.Stage?.Weight ?? 0) / 100);
            }

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

            if (!await _permissionService.CanAccessProjectAsync(User, record.ProjectId))
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
        public async Task<IActionResult> SaleTable(int? categoryId, List<int>? projectIds, List<int>? subProjectIds, DateTime? dateFrom, DateTime? dateTo)
        {
            projectIds ??= new List<int>();
            subProjectIds ??= new List<int>();
            var singleProjectId = projectIds.Count == 1 ? projectIds[0] : (int?)null;
            var accessibleProjects = await GetAccessibleProjectsAsync(categoryId);

            ViewBag.Categories = await GetActiveCategoriesAsync();
            ViewBag.Projects = accessibleProjects.Where(p => p.Scope == ProjectScope.Main).ToList();
            ViewBag.CategoryId = categoryId;
            ViewBag.ProjectIds = projectIds;
            ViewBag.ProjectId = singleProjectId;
            ViewBag.MainProjectId = singleProjectId;
            ViewBag.SubProjectIds = subProjectIds;
            ViewBag.SubProjects = singleProjectId.HasValue ? await GetSubProjectsAsync(singleProjectId) : new List<Project>();
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            List<int> targetProjectIds;
            Project? project = null;

            if (singleProjectId.HasValue)
            {
                project = await _context.Projects.FindAsync(singleProjectId.Value);
                if (project == null)
                    return NotFound();

                if (!await _permissionService.CanAccessProjectAsync(User, singleProjectId.Value))
                    return Forbid();

                if (subProjectIds.Any())
                {
                    foreach (var spid in subProjectIds)
                    {
                        if (!await _permissionService.CanAccessProjectAsync(User, spid))
                            return Forbid();
                    }
                    targetProjectIds = subProjectIds;
                }
                else
                {
                    var childIds = await _context.Projects.Where(p => p.ParentProjectId == singleProjectId.Value).Select(p => p.Id).ToListAsync();
                    targetProjectIds = new List<int> { singleProjectId.Value };
                    targetProjectIds.AddRange(childIds);
                }
            }
            else if (projectIds.Any())
            {
                foreach (var pid in projectIds)
                {
                    if (!await _permissionService.CanAccessProjectAsync(User, pid))
                        return Forbid();
                }
                targetProjectIds = projectIds;
            }
            else
            {
                targetProjectIds = accessibleProjects.Select(p => p.Id).ToList();
            }

            ViewBag.Project = project;

            var query = _context.FinancialClaims
                .Include(c => c.ProjectAssignment)
                .Include(c => c.Project)
                .Where(c => targetProjectIds.Contains(c.ProjectId));

            if (dateFrom.HasValue)
                query = query.Where(c => c.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(c => c.CreatedAt <= dateTo.Value.AddDays(1).AddTicks(-1));

            var claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            return View(claims);
        }

        [RequirePermission("Finance.Sales.View")]
        public async Task<IActionResult> Claims(int? categoryId, List<int>? projectIds, List<int>? subProjectIds, DateTime? dateFrom, DateTime? dateTo)
        {
            projectIds ??= new List<int>();
            subProjectIds ??= new List<int>();
            var singleProjectId = projectIds.Count == 1 ? projectIds[0] : (int?)null;
            var accessibleProjects = await GetAccessibleProjectsAsync(categoryId);

            ViewBag.Categories = await GetActiveCategoriesAsync();
            ViewBag.Projects = accessibleProjects.Where(p => p.Scope == ProjectScope.Main).ToList();
            ViewBag.CategoryId = categoryId;
            ViewBag.ProjectIds = projectIds;
            ViewBag.ProjectId = singleProjectId;
            ViewBag.MainProjectId = singleProjectId;
            ViewBag.SubProjectIds = subProjectIds;
            ViewBag.SubProjects = singleProjectId.HasValue ? await GetSubProjectsAsync(singleProjectId) : new List<Project>();
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            List<int> targetProjectIds;
            Project? project = null;

            if (singleProjectId.HasValue)
            {
                project = await _context.Projects.FindAsync(singleProjectId.Value);
                if (project == null)
                    return NotFound();

                if (!await _permissionService.CanAccessProjectAsync(User, singleProjectId.Value))
                    return Forbid();

                if (subProjectIds.Any())
                {
                    foreach (var spid in subProjectIds)
                    {
                        if (!await _permissionService.CanAccessProjectAsync(User, spid))
                            return Forbid();
                    }
                    targetProjectIds = subProjectIds;
                }
                else
                {
                    var childIds = await _context.Projects.Where(p => p.ParentProjectId == singleProjectId.Value).Select(p => p.Id).ToListAsync();
                    targetProjectIds = new List<int> { singleProjectId.Value };
                    targetProjectIds.AddRange(childIds);
                }
            }
            else if (projectIds.Any())
            {
                foreach (var pid in projectIds)
                {
                    if (!await _permissionService.CanAccessProjectAsync(User, pid))
                        return Forbid();
                }
                targetProjectIds = projectIds;
            }
            else
            {
                targetProjectIds = accessibleProjects.Select(p => p.Id).ToList();
            }

            ViewBag.Project = project;

            var query = _context.FinancialClaims
      .Include(c => c.ProjectAssignment).ThenInclude(a => a.Stage)
      .Include(c => c.Project)
      .Where(c => targetProjectIds.Contains(c.ProjectId));

            if (dateFrom.HasValue)
                query = query.Where(c => c.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(c => c.CreatedAt <= dateTo.Value.AddDays(1).AddTicks(-1));

            var claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            return View(claims);
        }

        [RequirePermission("Finance.Claims.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClaimPercentages(int id, int projectId, decimal? percent1, decimal? percent2)
        {
            var claim = await _context.FinancialClaims.FindAsync(id);
            if (claim == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, claim.ProjectId))
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
        public async Task<IActionResult> Expenses(int? categoryId, List<int>? projectIds, List<int>? subProjectIds, DateTime? dateFrom, DateTime? dateTo,
int? prefillSiteId, int? prefillCategoryId, string? prefillDescription, int? supplyRequestId)
        {
            projectIds ??= new List<int>();
            subProjectIds ??= new List<int>();
            var singleProjectId = projectIds.Count == 1 ? projectIds[0] : (int?)null;
            var accessibleProjects = await GetAccessibleProjectsAsync(categoryId);

            ViewBag.Categories = await GetActiveCategoriesAsync();
            ViewBag.Projects = accessibleProjects.Where(p => p.Scope == ProjectScope.Main).ToList();
            ViewBag.CategoryId = categoryId;
            ViewBag.ProjectIds = projectIds;
            ViewBag.ProjectId = singleProjectId;
            ViewBag.MainProjectId = singleProjectId;
            ViewBag.SubProjectIds = subProjectIds;
            ViewBag.SubProjects = singleProjectId.HasValue ? await GetSubProjectsAsync(singleProjectId) : new List<Project>();
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            List<int> targetProjectIds;
            Project? project = null;

            if (singleProjectId.HasValue)
            {
                project = await _context.Projects.FindAsync(singleProjectId.Value);
                if (project == null)
                    return NotFound();

                if (!await _permissionService.CanAccessProjectAsync(User, singleProjectId.Value))
                    return Forbid();

                if (subProjectIds.Any())
                {
                    foreach (var spid in subProjectIds)
                    {
                        if (!await _permissionService.CanAccessProjectAsync(User, spid))
                            return Forbid();
                    }
                    targetProjectIds = subProjectIds;
                }
                else
                {
                    var childIds = await _context.Projects.Where(p => p.ParentProjectId == singleProjectId.Value).Select(p => p.Id).ToListAsync();
                    targetProjectIds = new List<int> { singleProjectId.Value };
                    targetProjectIds.AddRange(childIds);
                }
            }
            else if (projectIds.Any())
            {
                foreach (var pid in projectIds)
                {
                    if (!await _permissionService.CanAccessProjectAsync(User, pid))
                        return Forbid();
                }
                targetProjectIds = projectIds;
            }
            else
            {
                targetProjectIds = accessibleProjects.Select(p => p.Id).ToList();
            }

            ViewBag.Project = project;

            var query = _context.ProjectExpenses
                .Include(e => e.ExpenseCategory)
                .Include(e => e.Site)
                .Where(e => e.ProjectId.HasValue && targetProjectIds.Contains(e.ProjectId.Value));

            if (dateFrom.HasValue)
                query = query.Where(e => e.Date >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(e => e.Date <= dateTo.Value);

            var expenses = await query.OrderByDescending(e => e.Date).ToListAsync();

            ViewBag.ExpenseCategories = await _context.ExpenseCategories.Where(c => c.IsActive && c.Scope == ExpenseCategoryScope.Project).OrderBy(c => c.NameAr).ToListAsync();
            ViewBag.Sites = singleProjectId.HasValue
                ? await _context.Sites.Where(s => s.ProjectId == singleProjectId.Value).OrderBy(s => s.Name).ToListAsync()
                : new List<Site>();
            ViewBag.PrefillSiteId = prefillSiteId;
            ViewBag.PrefillCategoryId = prefillCategoryId;
            ViewBag.PrefillDescription = prefillDescription;
            ViewBag.SupplyRequestId = supplyRequestId;

            return View(expenses);
        }

        [RequirePermission("Finance.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExpense([Bind("ProjectId,ExpenseCategoryId,SiteId,Amount,Date,Description")] ProjectExpense model, int? supplyRequestId)
        {
            if (!model.ProjectId.HasValue || !await _permissionService.CanAccessProjectAsync(User, model.ProjectId.Value))
                return Forbid();

            if (model.SiteId.HasValue)
            {
                var site = await _context.Sites.FindAsync(model.SiteId.Value);
                if (site == null || site.ProjectId != model.ProjectId.Value)
                {
                    TempData["Error"] = "الموقع المحدد لا يتبع هذا المشروع";
                    return RedirectToAction("Expenses", new { projectId = model.ProjectId });
                }
            }

            model.CreatedAt = DateTime.UtcNow;
            _context.ProjectExpenses.Add(model);
            await _context.SaveChangesAsync();

            if (supplyRequestId.HasValue)
            {
                var supplyRequest = await _context.SiteSupplyRequests.FindAsync(supplyRequestId.Value);
                if (supplyRequest != null && supplyRequest.Status == SiteSupplyStatus.Approved)
                {
                    supplyRequest.Status = SiteSupplyStatus.Delivered;
                    await _context.SaveChangesAsync();
                }
            }

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

            if (expense.ProjectId.HasValue && !await _permissionService.CanAccessProjectAsync(User, expense.ProjectId.Value))
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
        public async Task<IActionResult> Profit(int? categoryId, int? projectId, int? subProjectId, DateTime? dateFrom, DateTime? dateTo, int? genCategoryId, DateTime? genDateFrom, DateTime? genDateTo)
        {
            var effectiveProjectId = subProjectId ?? projectId;
            var accessibleProjects = await GetAccessibleProjectsAsync(categoryId);
            ViewBag.Categories = await GetActiveCategoriesAsync();
            ViewBag.Projects = accessibleProjects.Where(p => p.Scope == ProjectScope.Main).ToList();
            ViewBag.CategoryId = categoryId;
            ViewBag.ProjectId = effectiveProjectId;
            ViewBag.MainProjectId = projectId;
            ViewBag.SubProjectId = subProjectId;
            ViewBag.SubProjects = await GetSubProjectsAsync(projectId);
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            List<int> targetProjectIds;
            Project? project = null;

            if (effectiveProjectId.HasValue)
            {
                project = await _context.Projects.FindAsync(effectiveProjectId.Value);
                if (project == null)
                    return NotFound();

                if (!await _permissionService.CanAccessProjectAsync(User, effectiveProjectId.Value))
                    return Forbid();

                targetProjectIds = new List<int> { effectiveProjectId.Value };
            }
            else
            {
                targetProjectIds = accessibleProjects.Select(p => p.Id).ToList();
            }

            ViewBag.Project = project;

            // القسم المعزول: المصروفات الإدارية العامة — لا علاقة له بأي مشروع أو فلترته
            var genExpensesQuery = _context.ProjectExpenses.Include(e => e.ExpenseCategory).Where(e => e.ProjectId == null);
            if (genCategoryId.HasValue) genExpensesQuery = genExpensesQuery.Where(e => e.ExpenseCategoryId == genCategoryId.Value);
            if (genDateFrom.HasValue) genExpensesQuery = genExpensesQuery.Where(e => e.Date >= genDateFrom.Value);
            if (genDateTo.HasValue) genExpensesQuery = genExpensesQuery.Where(e => e.Date <= genDateTo.Value);
            var generalAdminExpenses = await genExpensesQuery.OrderByDescending(e => e.Date).ToListAsync();

            ViewBag.GeneralAdminExpenses = generalAdminExpenses;
            ViewBag.GeneralAdminTotal = generalAdminExpenses.Sum(e => e.Amount);
            ViewBag.GeneralAdminCategories = await _context.ExpenseCategories
                .Where(c => c.IsActive && c.Scope == ExpenseCategoryScope.General)
                .OrderBy(c => c.NameAr).ToListAsync();
            ViewBag.GenCategoryId = genCategoryId;
            ViewBag.GenDateFrom = genDateFrom;
            ViewBag.GenDateTo = genDateTo;

            // إجمالي المصروفات الإدارية ضمن نفس فترة الفلتر الرئيسي (من/إلى)، يُستخدم فقط لحساب هامش الربح
            var marginAdminExpensesQuery = _context.ProjectExpenses.Where(e => e.ProjectId == null);
            if (dateFrom.HasValue) marginAdminExpensesQuery = marginAdminExpensesQuery.Where(e => e.Date >= dateFrom.Value);
            if (dateTo.HasValue) marginAdminExpensesQuery = marginAdminExpensesQuery.Where(e => e.Date <= dateTo.Value);
            ViewBag.GeneralAdminTotalForMargin = await marginAdminExpensesQuery.SumAsync(e => e.Amount);

            // الخزينة وهامش الربح: دائماً على كل المشاريع ضمن التصنيف المختار، بغض النظر عن اختيار مشروع معيّن
            var aggregateProjectIds = accessibleProjects.Select(p => p.Id).ToList();
            ViewBag.Aggregate = aggregateProjectIds.Any()
                ? await _calc.CalculateProjectNetProfitAsync(aggregateProjectIds, dateFrom, dateTo)
                : new ProjectCalculationService.ProjectNetProfitResult();

            if (!targetProjectIds.Any())
                return View((ProjectCalculationService.ProjectNetProfitResult?)null);

            var result = await _calc.CalculateProjectNetProfitAsync(targetProjectIds, dateFrom, dateTo);
            return View(result);
        }
   
        [RequirePermission("Finance.Sales.View")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportSaleTableSelectedPdf(List<int> claimIds, int projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            if (claimIds == null || !claimIds.Any())
            {
                TempData["Error"] = "اختاري سطراً واحداً على الأقل للتصدير";
                return RedirectToAction("SaleTable", new { projectId, dateFrom, dateTo });
            }

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();
            if (!await _permissionService.CanAccessProjectAsync(User, projectId)) return Forbid();

            var claims = await _context.FinancialClaims
                .Include(c => c.ProjectAssignment)
                .Where(c => c.ProjectId == projectId && claimIds.Contains(c.Id))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var pdf = _pdfExport.GenerateClaimsReport("جدول البيع النهائي", new() { (project, claims) }, includePercentageColumns: true);
            return File(pdf, "application/pdf", $"جدول-البيع-{project.Code}.pdf");
        }

        [RequirePermission("Finance.Sales.View")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportClaimsSelectedPdf(List<int> claimIds, int projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            if (claimIds == null || !claimIds.Any())
            {
                TempData["Error"] = "اختاري سطراً واحداً على الأقل للتصدير";
                return RedirectToAction("Claims", new { projectId, dateFrom, dateTo });
            }

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();
            if (!await _permissionService.CanAccessProjectAsync(User, projectId)) return Forbid();

            var claims = await _context.FinancialClaims
                .Include(c => c.ProjectAssignment)
                .Where(c => c.ProjectId == projectId && claimIds.Contains(c.Id))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var pdf = _pdfExport.GenerateClaimsReport("المطالبة", new() { (project, claims) }, includePercentageColumns: false);
            return File(pdf, "application/pdf", $"المطالبة-{project.Code}.pdf");
        }

        [RequirePermission("Finance.Claims.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterClaimPayment(int claimId, int projectId, string returnAction = "Claims")
        {
            var claim = await _context.FinancialClaims.FindAsync(claimId);
            if (claim == null) return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, claim.ProjectId))
                return Forbid();

            var amountRaw = Request.Form[$"amount_{claimId}"];
            if (!decimal.TryParse(amountRaw, out var amount) || amount <= 0)
            {
                TempData["Error"] = "ادخلي مبلغاً صحيحاً أكبر من صفر";
                return RedirectToAction(returnAction == "Claims" ? "Claims" : "SaleTable", new { projectId });
            }

            var remaining = claim.RealValue - claim.PaidAmount;
            if (amount > remaining)
            {
                TempData["Error"] = "المبلغ المُدخل أكبر من المتبقي على هذه المطالبة";
                return RedirectToAction(returnAction == "Claims" ? "Claims" : "SaleTable", new { projectId });
            }

            var nextReceiptNumber = (await _context.ClaimPayments.MaxAsync(p => (int?)p.ReceiptNumber) ?? 0) + 1;

            claim.PaidAmount += amount;
            _context.ClaimPayments.Add(new ClaimPayment
            {
                FinancialClaimId = claim.Id,
                Amount = amount,
                PaidAt = DateTime.UtcNow,
                CreatedById = CurrentUserId,
                ReceiptNumber = nextReceiptNumber
            });
            if (claim.RealValue - claim.PaidAmount <= 0)
            {
                claim.IsClientSettled = true;
                claim.ClientSettledAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تسجيل الدفعة بنجاح";
            return RedirectToAction(returnAction == "Claims" ? "Claims" : "SaleTable", new { projectId });
        }
        [RequirePermission("Finance.Sales.View")]
             public async Task<IActionResult> ClaimPaymentsHistory(int claimId)
        {
            var claim = await _context.FinancialClaims
                .Include(c => c.Project)
                .Include(c => c.ProjectAssignment).ThenInclude(a => a.Stage)
                .FirstOrDefaultAsync(c => c.Id == claimId);
            if (claim == null) return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, claim.ProjectId))
                return Forbid();

            var payments = await _context.ClaimPayments
                .Where(p => p.FinancialClaimId == claimId)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();

            ViewBag.Claim = claim;
            return View("ClaimPayments", payments);
        }

        [RequirePermission("Finance.Claims.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClaimPayment(int id)
        {
            var payment = await _context.ClaimPayments.FindAsync(id);
            if (payment == null) return NotFound();

            var claim = await _context.FinancialClaims.FindAsync(payment.FinancialClaimId);
            if (claim == null) return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, claim.ProjectId))
                return Forbid();

            claim.PaidAmount -= payment.Amount;
            if (claim.PaidAmount < 0) claim.PaidAmount = 0;

            if (claim.RealValue - claim.PaidAmount > 0)
            {
                claim.IsClientSettled = false;
                claim.ClientSettledAt = null;
            }

            _context.ClaimPayments.Remove(payment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف الإيصال وإعادة قيمته إلى المطالبة";
            return RedirectToAction("ClaimPaymentsHistory", new { claimId = claim.Id });
        }

        [RequirePermission("Finance.Sales.View")]
        public async Task<IActionResult> ExportPaymentReceiptPdf(int paymentId)
        {
            var payment = await _context.ClaimPayments
         .Include(p => p.FinancialClaim).ThenInclude(c => c.Project).ThenInclude(p => p.Client)
         .Include(p => p.FinancialClaim).ThenInclude(c => c.ProjectAssignment).ThenInclude(a => a.Stage)
         .FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null) return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, payment.FinancialClaim.ProjectId))
                return Forbid();

            var previousPayments = await _context.ClaimPayments
        .Where(p => p.FinancialClaimId == payment.FinancialClaimId
            && (p.PaidAt < payment.PaidAt || (p.PaidAt == payment.PaidAt && p.Id < payment.Id)))
        .OrderBy(p => p.PaidAt)
        .ToListAsync();

            var pdf = _pdfExport.GeneratePaymentReceipt(payment, previousPayments);
            return File(pdf, "application/pdf", $"إيصال-دفعة-{payment.Id}.pdf");
        }

        [RequirePermission("Finance.Sales.View")]
        public async Task<IActionResult> PrintProjectInvoice(int projectId)
        {
            var project = await _context.Projects.Include(p => p.Client).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId))
                return Forbid();

            var claims = await _context.FinancialClaims
     .Include(c => c.ProjectAssignment).ThenInclude(a => a.Stage)
     .Where(c => c.ProjectId == projectId)
     .OrderBy(c => c.CreatedAt)
     .ToListAsync();

            if (!claims.Any())
            {
                TempData["Error"] = "لا توجد مطالبات لهذا المشروع لإصدار فاتورة";
                return RedirectToAction("Claims", new { projectId });
            }

            var pdf = _pdfExport.GenerateProjectInvoice(project, claims);
            return File(pdf, "application/pdf", $"فاتورة-{project.Code}.pdf");
        }

        [RequirePermission("Finance.Sales.View")]
        public async Task<IActionResult> PrintProfitReport(int projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId))
                return Forbid();

            var result = await _calc.CalculateProjectNetProfitAsync(new List<int> { projectId }, dateFrom, dateTo);

            var pdf = _pdfExport.GenerateProjectProfitReport(project, result);
            return File(pdf, "application/pdf", $"تقرير-الربح-{project.Code}.pdf");
        }

        [RequirePermission("Finance.Sales.View")]
        public async Task<IActionResult> Receipts(int? categoryId, int? clientId, List<int>? projectIds, DateTime? dateFrom, DateTime? dateTo)
        {
            var accessibleProjects = await GetAccessibleProjectsAsync(categoryId);
            ViewBag.Categories = await GetActiveCategoriesAsync();
            ViewBag.Projects = accessibleProjects;
            ViewBag.Clients = await _context.Clients.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.CategoryId = categoryId;
            ViewBag.ClientId = clientId;
            ViewBag.ProjectIds = projectIds ?? new List<int>();
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            var accessibleProjectIds = accessibleProjects.Select(p => p.Id).ToList();

            if (clientId.HasValue)
                accessibleProjectIds = accessibleProjects.Where(p => p.ClientId == clientId.Value).Select(p => p.Id).ToList();

            if (projectIds != null && projectIds.Any())
                accessibleProjectIds = accessibleProjectIds.Where(id => projectIds.Contains(id)).ToList();

            var query = _context.ClaimPayments
                .Include(p => p.FinancialClaim).ThenInclude(c => c.Project).ThenInclude(p => p.Client)
                .Include(p => p.FinancialClaim).ThenInclude(c => c.ProjectAssignment)
                .Where(p => accessibleProjectIds.Contains(p.FinancialClaim.ProjectId));

            if (dateFrom.HasValue) query = query.Where(p => p.PaidAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(p => p.PaidAt <= dateTo.Value.AddDays(1).AddTicks(-1));

            var payments = await query.OrderByDescending(p => p.PaidAt).ToListAsync();
            return View(payments);
        }

    }
}