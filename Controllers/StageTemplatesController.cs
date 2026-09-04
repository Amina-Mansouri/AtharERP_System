using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    public class StageTemplatesController : Controller
    {
        private readonly AppDbContext _context;

        public StageTemplatesController(AppDbContext context)
        {
            _context = context;
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string? tasks)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "اسم القالب مطلوب";
                return RedirectToAction("Create");
            }

            var template = new StageTemplate
            {
                Name = name.Trim(),
                IsActive = true,
                Order = await _context.StageTemplates.CountAsync() + 1
            };
            _context.StageTemplates.Add(template);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(tasks))
            {
                var order = 1;
                foreach (var line in tasks.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var taskName = line.Trim();
                    if (string.IsNullOrEmpty(taskName)) continue;

                    _context.StageTemplateTasks.Add(new StageTemplateTask
                    {
                        StageTemplateId = template.Id,
                        TaskName = taskName,
                        Order = order
                    });
                    order++;
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"تمت إضافة قالب {template.Name} بنجاح";
            return RedirectToAction("Details", "Projects", new { id = Request.Query["projectId"] });
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var template = await _context.StageTemplates.Include(t => t.DefaultTasks).FirstOrDefaultAsync(t => t.Id == id);
            if (template == null)
                return NotFound();

            return View(template);
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, string? tasks, bool isActive)
        {
            var template = await _context.StageTemplates.Include(t => t.DefaultTasks).FirstOrDefaultAsync(t => t.Id == id);
            if (template == null)
                return NotFound();

            template.Name = name.Trim();
            template.IsActive = isActive;

            _context.StageTemplateTasks.RemoveRange(template.DefaultTasks);

            if (!string.IsNullOrWhiteSpace(tasks))
            {
                var order = 1;
                foreach (var line in tasks.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var taskName = line.Trim();
                    if (string.IsNullOrEmpty(taskName)) continue;

                    _context.StageTemplateTasks.Add(new StageTemplateTask
                    {
                        StageTemplateId = template.Id,
                        TaskName = taskName,
                        Order = order
                    });
                    order++;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم تحديث قالب {template.Name} بنجاح";
            return RedirectToAction("Index", "StageTemplates");
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var templates = await _context.StageTemplates
                .Include(t => t.DefaultTasks)
                .OrderBy(t => t.Order)
                .ToListAsync();

            return View(templates);
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _context.StageTemplates.FindAsync(id);
            if (template != null)
            {
                _context.StageTemplates.Remove(template);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "تم حذف القالب بنجاح";
            return RedirectToAction("Index");
        }
    }
}