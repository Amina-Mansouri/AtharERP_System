using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    public class ClientRedesignRequestsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly ProjectCalculationService _calc;

        public ClientRedesignRequestsController(AppDbContext context, PermissionService permissionService, ProjectCalculationService calc)
        {
            _context = context;
            _permissionService = permissionService;
            _calc = calc;
        }

        [RequirePermission("Projects.Stages.Manage")]
        public async Task<IActionResult> Index(int stageId)
        {
            var stage = await _context.ProjectStages.Include(s => s.Project).FirstOrDefaultAsync(s => s.Id == stageId);
            if (stage == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, stage.ProjectId))
                return Forbid();

            var requests = await _context.ClientRedesignRequests
                .Where(r => r.ProjectStageId == stageId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            ViewBag.Stage = stage;
            return View(requests);
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectStageId,IncreasePercentage,Notes")] ClientRedesignRequest model)
        {
            var stage = await _context.ProjectStages.FindAsync(model.ProjectStageId);
            if (stage == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, stage.ProjectId))
                return Forbid();

            model.RequestDate = DateTime.UtcNow;
            model.CreatedAt = DateTime.UtcNow;

            _context.ClientRedesignRequests.Add(model);
            await _context.SaveChangesAsync();

            await _calc.SyncStageRedesignPercentageAsync(model.ProjectStageId);

            TempData["Success"] = "تم تسجيل طلب إعادة التصميم بنجاح";
            return RedirectToAction("Index", new { stageId = model.ProjectStageId });
        }

        [RequirePermission("Projects.Stages.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.ClientRedesignRequests
                .Include(r => r.ProjectStage)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, request.ProjectStage.ProjectId))
                return Forbid();

            var stageId = request.ProjectStageId;
            _context.ClientRedesignRequests.Remove(request);
            await _context.SaveChangesAsync();

            await _calc.SyncStageRedesignPercentageAsync(stageId);

            TempData["Success"] = "تم حذف الطلب بنجاح";
            return RedirectToAction("Index", new { stageId });
        }
    }
}