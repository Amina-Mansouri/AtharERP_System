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

        public FinanceController(AppDbContext context, PermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task<List<Project>> GetAccessibleProjectsAsync()
        {
            var canViewAll = await _permissionService.HasPermissionAsync(User, "Projects.ViewAll");
            var myProjectIds = await _context.ProjectTeamMembers
                .Where(tm => tm.UserId == CurrentUserId)
                .Select(tm => tm.ProjectId)
                .ToListAsync();

            var query = _context.Projects.AsQueryable();
            if (!canViewAll)
                query = query.Where(p => p.CreatedById == CurrentUserId || myProjectIds.Contains(p.Id));

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        // ============================================
        // جدول التكاليف — داخلي، لحساب مستحقات المهندسات
        // ============================================
        [RequirePermission("Finance.Costs.View")]
        public async Task<IActionResult> CostTable(int? projectId)
        {
            ViewBag.Projects = await GetAccessibleProjectsAsync();
            ViewBag.ProjectId = projectId;

            if (!projectId.HasValue)
                return View(new List<FinancialRecord>());

            var project = await _context.Projects.FindAsync(projectId.Value);
            if (project == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId.Value))
                return Forbid();

            var records = await _context.FinancialRecords
                .Include(r => r.ProjectAssignment)
                .Include(r => r.Engineer)
                .Where(r => r.ProjectId == projectId.Value)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // نسبة المهمة من المشروع لكل مهندسة: مجموع (وزن المهمة % من مرحلتها × وزن المرحلة % من المشروع) عبر كل مهامها المُعدَّة (DesignProposal.PreparedById) في كامل هذا المشروع
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
        public async Task<IActionResult> SaleTable(int? projectId)
        {
            ViewBag.Projects = await GetAccessibleProjectsAsync();
            ViewBag.ProjectId = projectId;

            if (!projectId.HasValue)
                return View(new List<FinancialClaim>());

            var project = await _context.Projects.FindAsync(projectId.Value);
            if (project == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, projectId.Value))
                return Forbid();

            var claims = await _context.FinancialClaims
                .Include(c => c.ProjectAssignment)
                .Where(c => c.ProjectId == projectId.Value)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Project = project;
            return View(claims);
        }

        [RequirePermission("Finance.Claims.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkClaimSettled(int id, int projectId)
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

            return RedirectToAction("SaleTable", new { projectId });
        }
    }
}