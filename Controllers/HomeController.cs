using AtharERP_System.Data;
using AtharERP_System.Models;
using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AtharERP_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AppDbContext _context;

        public HomeController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            ViewBag.TotalRoles = await _roleManager.Roles.CountAsync();
            ViewBag.TotalPermissions = await _context.Permissions.CountAsync();

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