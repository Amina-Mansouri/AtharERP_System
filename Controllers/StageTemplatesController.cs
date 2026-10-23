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

        private async Task<List<Department>> LoadStageDisciplinesAsync()
        {
            return await _context.Departments
                .Where(d => d.ParentDepartment != null && d.ParentDepartment.IsStageDisciplinesParent && d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Disciplines = await LoadStageDisciplinesAsync();
            return View();
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, decimal? defaultWeight, List<string>? taskNames, List<decimal?>? taskWeights, int? disciplineDepartmentId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ViewBag.Error = "اسم القالب مطلوب";
                ViewBag.PostedName = name;
                ViewBag.Disciplines = await LoadStageDisciplinesAsync();
                return View();
            }

            var totalTaskWeight = taskWeights?.Where(w => w.HasValue).Sum(w => w!.Value) ?? 0;
            if (totalTaskWeight > 100)
            {
                ViewBag.Error = $"مجموع أوزان المهام الافتراضية ({totalTaskWeight:N2}%) يتجاوز 100%";
                ViewBag.PostedName = name;
                ViewBag.PostedDefaultWeight = defaultWeight;
                ViewBag.PostedDisciplineId = disciplineDepartmentId;
                ViewBag.PostedTaskNames = taskNames;
                ViewBag.PostedTaskWeights = taskWeights;
                ViewBag.Disciplines = await LoadStageDisciplinesAsync();
                return View();
            }

            var template = new StageTemplate
            {
                Name = name.Trim(),
                IsActive = true,
                Order = await _context.StageTemplates.CountAsync() + 1,
                DisciplineDepartmentId = disciplineDepartmentId,
                DefaultWeight = defaultWeight
            };
            _context.StageTemplates.Add(template);
            await _context.SaveChangesAsync();

            if (taskNames != null)
            {
                var order = 1;
                for (int i = 0; i < taskNames.Count; i++)
                {
                    var taskName = taskNames[i]?.Trim();
                    if (string.IsNullOrEmpty(taskName)) continue;

                    _context.StageTemplateTasks.Add(new StageTemplateTask
                    {
                        StageTemplateId = template.Id,
                        TaskName = taskName,
                        Order = order,
                        DefaultWeight = (taskWeights != null && i < taskWeights.Count) ? taskWeights[i] : null
                    });
                    order++;
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"تمت إضافة قالب {template.Name} بنجاح";
            return RedirectToAction("Index", "StageTemplates");

        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var template = await _context.StageTemplates.Include(t => t.DefaultTasks).FirstOrDefaultAsync(t => t.Id == id);
            if (template == null)
                return NotFound();

            ViewBag.Disciplines = await LoadStageDisciplinesAsync();
            return View(template);
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, decimal? defaultWeight, List<string>? taskNames, List<decimal?>? taskWeights, bool isActive, int? disciplineDepartmentId)
        {
            var template = await _context.StageTemplates.Include(t => t.DefaultTasks).FirstOrDefaultAsync(t => t.Id == id);
            if (template == null)
                return NotFound();

            var totalTaskWeight = taskWeights?.Where(w => w.HasValue).Sum(w => w!.Value) ?? 0;
            if (totalTaskWeight > 100)
            {
                ViewBag.Error = $"مجموع أوزان المهام الافتراضية ({totalTaskWeight:N2}%) يتجاوز 100%";
                ViewBag.Disciplines = await LoadStageDisciplinesAsync();
                template.Name = name;
                template.DefaultWeight = defaultWeight;
                template.DisciplineDepartmentId = disciplineDepartmentId;
                template.IsActive = isActive;
                ViewBag.PostedTaskNames = taskNames;
                ViewBag.PostedTaskWeights = taskWeights;
                return View(template);
            }

            template.Name = name.Trim();
            template.IsActive = isActive;
            template.DisciplineDepartmentId = disciplineDepartmentId;
            template.DefaultWeight = defaultWeight;

            _context.StageTemplateTasks.RemoveRange(template.DefaultTasks);

            if (taskNames != null)
            {
                var order = 1;
                for (int i = 0; i < taskNames.Count; i++)
                {
                    var taskName = taskNames[i]?.Trim();
                    if (string.IsNullOrEmpty(taskName)) continue;

                    _context.StageTemplateTasks.Add(new StageTemplateTask
                    {
                        StageTemplateId = template.Id,
                        TaskName = taskName,
                        Order = order,
                        DefaultWeight = (taskWeights != null && i < taskWeights.Count) ? taskWeights[i] : null
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
                .Include(t => t.Discipline)
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