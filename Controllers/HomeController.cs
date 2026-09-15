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
            var user = await _context.Users.Include(u => u.Department).Include(u => u.JobRankRef).ThenInclude(r => r!.CareerTrack).FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            var myAssignments = await _context.ProjectAssignments
                .Include(a => a.Stage).ThenInclude(s => s.Project)
                .Include(a => a.Tasks)
                .Where(a => a.Engineers.Any(e => e.UserId == CurrentUserId))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var activeAssignments = myAssignments
                .Where(a => a.Status != AssignmentStatus.Completed && a.Status != AssignmentStatus.Cancelled)
                .ToList();

            var myAssignmentIds = myAssignments.Select(a => a.Id).ToList();
            var myTasks = await _context.ProjectTasks
                .Include(t => t.Todos)
                .Where(t => t.Assignees.Any(x => x.UserId == CurrentUserId) || (t.ProjectAssignmentId.HasValue && myAssignmentIds.Contains(t.ProjectAssignmentId.Value)))
                .ToListAsync();

            var openTasks = myTasks.Where(t => t.Status != ProjectTaskStatus.Completed).OrderBy(t => t.PlannedEndDate).ToList();
            var today = DateTime.UtcNow.Date;

            ViewBag.CurrentUser = user;
            ViewBag.MyProjectsCount = activeAssignments.Select(a => a.Stage!.ProjectId).Distinct().Count();
            ViewBag.MyAssignmentsCount = activeAssignments.Count;
            ViewBag.DelayedAssignmentsCount = activeAssignments.Count(a => a.Tasks.Any(t => t.DelayDays > 0));
            ViewBag.AvgCompletion = myTasks.Any() ? Math.Round(myTasks.Average(t => t.CompletionPercentage), 0) : 0;
            ViewBag.MyAssignmentsList = activeAssignments;
            ViewBag.OpenTasks = openTasks;
            ViewBag.CompletedAssignmentsCount = myAssignments.Count(a => a.Status == AssignmentStatus.Completed);

            var completedTasks = myTasks.Where(t => t.Status == ProjectTaskStatus.Completed).ToList();
            ViewBag.EarlyDeliveryRate = completedTasks.Any() ? Math.Round(completedTasks.Count(t => t.EarlyDeliveryDays > 0) * 100m / completedTasks.Count, 0) : 0;
            ViewBag.DelayRate = completedTasks.Any() ? Math.Round(completedTasks.Count(t => t.DelayDays > 0) * 100m / completedTasks.Count, 0) : 0;

            ViewBag.DesignedArea = myAssignments
                .Where(a => a.Status == AssignmentStatus.Completed && a.Stage != null)
                .Select(a => a.Stage!)
                .Distinct()
                .Sum(s => s.Area ?? 0);

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            // ========== الوحدة 01: نظرة عامة على الشركة (لمن يملك Users.View فقط) ==========
            bool canViewUsers = await _permissionService.HasPermissionAsync(User, "Users.View");
            ViewBag.CanViewModule01 = canViewUsers;

            if (canViewUsers)
            {
                ViewBag.LatestNotifications = await _context.Notifications
                    .Include(n => n.User)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(8)
                    .ToListAsync();

                ViewBag.GovernmentalProjects = await _context.Projects.CountAsync(p => p.Type == ProjectType.Governmental);
                ViewBag.MunicipalProjects = await _context.Projects.CountAsync(p => p.Type == ProjectType.Municipal);
                ViewBag.PrivateProjects = await _context.Projects.CountAsync(p => p.Type == ProjectType.Private);
                ViewBag.InvestmentProjects = await _context.Projects.CountAsync(p => p.Type == ProjectType.Investment);
                ViewBag.UnclassifiedProjects = await _context.Projects.CountAsync(p => p.Type == null);

                ViewBag.LowPriorityProjects = await _context.Projects.CountAsync(p => p.Priority == Priority.Low);
                ViewBag.NormalPriorityProjects = await _context.Projects.CountAsync(p => p.Priority == Priority.Normal);
                ViewBag.HighPriorityProjects = await _context.Projects.CountAsync(p => p.Priority == Priority.High);
                ViewBag.CriticalPriorityProjects = await _context.Projects.CountAsync(p => p.Priority == Priority.Critical);
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
                ViewBag.OnHoldSites = await sitesQuery.CountAsync(s => s.Status == SiteStatus.OnHold);
                ViewBag.CompletedSites = await sitesQuery.CountAsync(s => s.Status == SiteStatus.Completed);
                ViewBag.PendingQualityChecks = await _context.SiteQualityChecks
                    .CountAsync(q => siteIds.Contains(q.SiteId) && !q.IsApproved);
                ViewBag.PendingSupplyRequests = await _context.SiteSupplyRequests
                    .CountAsync(r => siteIds.Contains(r.SiteId) && r.Status == SiteSupplyStatus.Pending);
            }

            // ========== الوحدة 04: تكليفات المشاريع (لمن يملك Projects.Assignments.View) ==========
            bool canViewAssignments = await _permissionService.HasPermissionAsync(User, "Projects.Assignments.View");
            ViewBag.CanViewModule04 = canViewAssignments;

            if (canViewAssignments)
            {
                var assignmentsQuery = _context.ProjectAssignments.Include(a => a.Tasks).AsQueryable();

                if (!canViewAllProjects)
                {
                    var myProjectIds = await _context.ProjectTeamMembers
                        .Where(tm => tm.UserId == CurrentUserId)
                        .Select(tm => tm.ProjectId)
                        .ToListAsync();

                    assignmentsQuery = assignmentsQuery.Where(a => a.Project.CreatedById == CurrentUserId || myProjectIds.Contains(a.ProjectId));
                }

                var allAssignments = await assignmentsQuery.ToListAsync();

                ViewBag.TotalAssignments = allAssignments.Count;
                ViewBag.PendingAssignments = allAssignments.Count(a => a.Status == AssignmentStatus.Pending && !a.Tasks.Any(t => t.DelayDays > 0));
                ViewBag.InProgressAssignments = allAssignments.Count(a => a.Status == AssignmentStatus.InProgress && !a.Tasks.Any(t => t.DelayDays > 0));
                ViewBag.OverdueAssignmentsCount = allAssignments.Count(a => a.Status != AssignmentStatus.Completed && a.Status != AssignmentStatus.Cancelled && a.Tasks.Any(t => t.DelayDays > 0));
                ViewBag.CompletedAssignments = allAssignments.Count(a => a.Status == AssignmentStatus.Completed);
                ViewBag.CancelledAssignments = allAssignments.Count(a => a.Status == AssignmentStatus.Cancelled);
            }

            // ========== الوحدة 05: تحليلات المراحل (لمن يملك ViewAll أو ViewOwn للمشاريع) ==========
            ViewBag.CanViewModule05 = canViewProjects;

            if (canViewProjects)
            {
                var stagesQuery = _context.ProjectStages.Include(s => s.Project).Include(s => s.Tasks).AsQueryable();

                if (!canViewAllProjects)
                {
                    var myProjectIds = await _context.ProjectTeamMembers
                        .Where(tm => tm.UserId == CurrentUserId)
                        .Select(tm => tm.ProjectId)
                        .ToListAsync();

                    stagesQuery = stagesQuery.Where(s => s.Project.CreatedById == CurrentUserId || myProjectIds.Contains(s.ProjectId));
                }

                var allStagesForAnalytics = await stagesQuery.ToListAsync();

                ViewBag.AvgDurationByStageName = allStagesForAnalytics
                    .Where(s => s.Status == StageStatus.Completed && s.PlannedStartDate.HasValue && s.ActualDeliveryDate.HasValue)
                    .GroupBy(s => s.Name)
                    .Select(g => (Name: g.Key, AvgDays: g.Average(s => (s.ActualDeliveryDate!.Value - s.PlannedStartDate!.Value).TotalDays)))
                    .OrderByDescending(x => x.AvgDays)
                    .Take(6)
                    .ToList();

                ViewBag.MostDelayedStages = allStagesForAnalytics
                    .Where(s => s.PlannedEndDate.HasValue && s.Status != StageStatus.New)
                    .Select(s => (StageName: s.Name, ProjectName: s.Project.Name, DelayDays: ((s.ActualDeliveryDate ?? DateTime.UtcNow.Date) - s.PlannedEndDate!.Value).Days))
                    .Where(x => x.DelayDays > 0)
                    .OrderByDescending(x => x.DelayDays)
                    .Take(5)
                    .ToList();

                ViewBag.MostReworkedStages = allStagesForAnalytics
                    .Select(s => (StageName: s.Name, ProjectName: s.Project.Name, ReworkCount: s.Tasks.Sum(t => t.RejectionCount)))
                    .Where(x => x.ReworkCount > 0)
                    .OrderByDescending(x => x.ReworkCount)
                    .Take(5)
                    .ToList();
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