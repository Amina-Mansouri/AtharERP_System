using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    [Authorize(Roles = "مدير النظام")]
    public class EmployeeDocumentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FileUploadService _fileUpload;
        private readonly AuditService _audit;

        public EmployeeDocumentsController(AppDbContext context, FileUploadService fileUpload, AuditService audit)
        {
            _context = context;
            _fileUpload = fileUpload;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة مستندات موظف معيّن
        // ============================================
        public async Task<IActionResult> Index(string userId)
        {
            var employee = await _context.Users.FindAsync(userId);
            if (employee == null)
                return NotFound();

            var documents = await _context.EmployeeDocuments
                .Include(d => d.UploadedBy)
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            ViewBag.Employee = employee;
            return View(documents);
        }

        // ============================================
        // رفع مستند
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string userId, IFormFile file, string? description)
        {
            var employee = await _context.Users.FindAsync(userId);
            if (employee == null)
                return NotFound();

            var result = await _fileUpload.SaveFileAsync(file, $"users/{userId}/documents");
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Index", new { userId });
            }

            var document = new EmployeeDocument
            {
                UserId = userId,
                FileName = file.FileName,
                FilePath = result.FilePath!,
                FileType = result.FileType,
                FileSize = result.FileSize,
                Description = description,
                UploadedAt = DateTime.UtcNow,
                UploadedById = CurrentUserId
            };

            _context.EmployeeDocuments.Add(document);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Upload", nameof(EmployeeDocument), document.Id.ToString(), $"رفع مستند لموظف: {employee.FullName} ({document.FileName})");

            TempData["Success"] = "تم رفع المستند بنجاح";
            return RedirectToAction("Index", new { userId });
        }

        // ============================================
        // حذف مستند
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.EmployeeDocuments.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
            if (document == null)
                return NotFound();

            var userId = document.UserId;
            var fileName = document.FileName;
            var employeeName = document.User.FullName;

            _fileUpload.DeleteFile(document.FilePath);
            _context.EmployeeDocuments.Remove(document);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(EmployeeDocument), id.ToString(), $"حذف مستند لموظف: {employeeName} ({fileName})");

            TempData["Success"] = "تم حذف المستند بنجاح";
            return RedirectToAction("Index", new { userId });
        }
    }
}