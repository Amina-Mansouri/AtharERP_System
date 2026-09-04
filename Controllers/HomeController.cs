using AtharERP_System.Data;
using AtharERP_System.Models;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;

        public HomeController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            AppDbContext context,
            PermissionService permissionService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _permissionService = permissionService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index()
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
                return RedirectToAction("Login", "Account");

            if (await _userManager.IsInRoleAsync((await _userManager.GetUserAsync(User))!, "مدير النظام"))
                return RedirectToAction("Dashboard");

            return RedirectToAction("EngineerDashboard");
        }

        [Authorize]
        public async Task<IActionResult> EngineerDashboard()
        {
            var myAssignments = await _context.ProjectAssignments
                .Include(a => a.Stage).ThenInclude(s => s.Project)
                .Where(a => a.LeadEngineerId == CurrentUserId || a.AssistantEngineerId == CurrentUserId)
                .Where(a => a.Status != AssignmentStatus.Completed && a.Status != AssignmentStatus.Cancelled)
                .OrderBy(a => a.AgreedDate)
                .ToListAsync();

            var myTasks = await _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.Stage)
                .Where(t => t.Assignees.Any(x => x.UserId == CurrentUserId))
                .Where(t => t.Status != ProjectTaskStatus.Completed)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            ViewBag.MyAssignments = myAssignments;
            ViewBag.MyTasks = myTasks;
            ViewBag.TodayTasks = myTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.UtcNow.Date);
            ViewBag.OverdueTasks = myTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.UtcNow.Date);
            ViewBag.MyProjectsCount = myAssignments.Select(a => a.Stage.ProjectId).Distinct().Count();

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            // ========== الوحدة 01: الهوية والصلاحيات (لمن يملك Users.View فقط) ==========
            bool canViewUsers = await _permissionService.HasPermissionAsync(User, "Users.View");
            ViewBag.CanViewModule01 = canViewUsers;

            if (canViewUsers)
            {
                ViewBag.TotalUsers = await _userManager.Users.CountAsync();
                ViewBag.ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive);
                ViewBag.TotalRoles = await _roleManager.Roles.CountAsync();
                ViewBag.TotalDepartments = await _context.Departments.CountAsync(d => d.IsActive);

                ViewBag.LatestUsers = await _userManager.Users
                    .Include(u => u.Department)
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                var deptCounts = await _context.Departments
                    .Where(d => d.IsActive)
                    .Select(d => new { d.Name, Count = d.Users.Count(u => u.IsActive) })
                    .OrderByDescending(d => d.Count)
                    .ToListAsync();

                ViewBag.DepartmentDistribution = deptCounts
                    .Select(d => new KeyValuePair<string, int>(d.Name, d.Count))
                    .ToList();
            }

            // ========== الوحدة 02: إدارة المشاريع (لمن يملك ViewAll أو ViewOwn) ==========
            bool canViewAllProjects = await _permissionService.HasPermissionAsync(User, "Projects.ViewAll");
            bool canViewOwnProjects = await _permissionService.HasPermissionAsync(User, "Projects.ViewOwn");
            bool canViewProjects = canViewAllProjects || canViewOwnProjects;
            ViewBag.CanViewModule02 = canViewProjects;
            ViewBag.ProjectsScopeIsAll = canViewAllProjects;

            if (canViewProjects)
            {
                var projectsQuery = _context.Projects.AsQueryable();

                if (!canViewAllProjects)
                {
                    var myProjectIds = await _context.ProjectTeamMembers
                        .Where(tm => tm.UserId == CurrentUserId)
                        .Select(tm => tm.ProjectId)
                        .ToListAsync();

                    projectsQuery = projectsQuery.Where(p => p.CreatedById == CurrentUserId || myProjectIds.Contains(p.Id));
                }

                ViewBag.TotalProjects = await projectsQuery.CountAsync();
                ViewBag.ActiveProjects = await projectsQuery.CountAsync(p => p.Status == ProjectStatus.InProgress);
                ViewBag.CompletedProjects = await projectsQuery.CountAsync(p => p.Status == ProjectStatus.Completed);

                ViewBag.MyActiveTasks = await _context.TaskAssignees
                    .Where(a => a.UserId == CurrentUserId && a.Task.Status != ProjectTaskStatus.Completed)
                    .CountAsync();
            }

            // ========== الوحدة 03: إدارة المواقع (لمن يملك Sites.View) ==========
            bool canViewSites = await _permissionService.HasPermissionAsync(User, "Sites.View");
            ViewBag.CanViewModule03 = canViewSites;

            if (canViewSites)
            {
                var sitesQuery = _context.Sites.AsQueryable();

                if (!canViewAllProjects)
                {
                    var myProjectIds = await _context.ProjectTeamMembers
                        .Where(tm => tm.UserId == CurrentUserId)
                        .Select(tm => tm.ProjectId)
                        .ToListAsync();

                    sitesQuery = sitesQuery.Where(s => s.Project.CreatedById == CurrentUserId || myProjectIds.Contains(s.ProjectId));
                }

                var siteIds = await sitesQuery.Select(s => s.Id).ToListAsync();

                ViewBag.TotalSites = siteIds.Count;
                ViewBag.ActiveSites = await sitesQuery.CountAsync(s => s.Status == SiteStatus.Active);
                ViewBag.PendingQualityChecks = await _context.SiteQualityChecks
                    .CountAsync(q => siteIds.Contains(q.SiteId) && !q.IsApproved);
                ViewBag.PendingSupplyRequests = await _context.SiteSupplyRequests
                    .CountAsync(r => siteIds.Contains(r.SiteId) && r.Status == SiteSupplyStatus.Pending);
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}