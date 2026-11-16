using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class PerformanceEvaluationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PerformanceEvaluationUploadService _uploadService;
        private readonly IWebHostEnvironment _environment;

        public PerformanceEvaluationController(AppDbContext context, PerformanceEvaluationUploadService uploadService, IWebHostEnvironment environment)
        {
            _context = context;
            _uploadService = uploadService;
            _environment = environment;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("HR.Evaluation")]
        public async Task<IActionResult> Index()
        {
            var uploads = await _context.PerformanceEvaluationUploads
                .Include(u => u.UploadedBy)
                .OrderByDescending(u => u.Year).ThenByDescending(u => u.Month)
                .ToListAsync();
            return View(uploads);
        }

        [RequirePermission("HR.Evaluation")]
        public IActionResult Upload()
        {
            return View();
        }

        [RequirePermission("HR.Evaluation")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string period, IFormFile file)
        {
            if (string.IsNullOrEmpty(period) || !DateTime.TryParse(period + "-01", out var parsed))
            {
                TempData["Error"] = "يجب تحديد الشهر";
                return RedirectToAction("Upload");
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "يجب اختيار ملف";
                return RedirectToAction("Upload");
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var result = await _uploadService.ProcessUploadAsync(ms.ToArray(), parsed.Year, parsed.Month, CurrentUserId);

            if (!result.Success)
            {
                TempData["Error"] = "فشل التحقق من الملف:\n" + string.Join("\n", result.Errors);
                return RedirectToAction("Upload");
            }

            TempData["Success"] = $"تم رفع الملف بنجاح — {result.EmployeesProcessed} موظفة";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Evaluation")]
        public async Task<IActionResult> Download(int id)
        {
            var upload = await _context.PerformanceEvaluationUploads.FirstOrDefaultAsync(u => u.Id == id);
            if (upload == null) return NotFound();

            var fullPath = Path.Combine(_environment.WebRootPath, upload.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return NotFound();
            return PhysicalFile(fullPath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"تقييم-{upload.Year}-{upload.Month:D2}.xlsx");
        }
    }
}