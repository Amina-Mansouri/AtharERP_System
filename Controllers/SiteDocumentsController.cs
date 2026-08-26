using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteDocumentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FileUploadService _fileUpload;
        private readonly AuditService _audit;

        public SiteDocumentsController(AppDbContext context, FileUploadService fileUpload, AuditService audit)
        {
            _context = context;
            _fileUpload = fileUpload;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ============================================
        // قائمة مستندات موقع معيّن (مصنَّفة حسب النوع)
        // ============================================
        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Index(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            var documents = await _context.SiteDocuments
                .Where(d => d.SiteId == siteId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            ViewBag.Site = site;
            return View(documents);
        }

        // ============================================
        // رفع مستند
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int siteId, SiteDocumentType documentType, IFormFile file, string? description)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
                return NotFound();

            var result = await _fileUpload.SaveFileAsync(file, $"sites/{siteId}/documents");
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Index", new { siteId });
            }

            var document = new SiteDocument
            {
                SiteId = siteId,
                DocumentType = documentType,
                FileName = file.FileName,
                FilePath = result.FilePath!,
                Description = description,
                UploadedAt = DateTime.UtcNow
            };

            _context.SiteDocuments.Add(document);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Upload", nameof(SiteDocument), document.Id.ToString(), $"رفع مستند موقع: {document.FileName}");

            TempData["Success"] = "تم رفع المستند بنجاح";
            return RedirectToAction("Index", new { siteId });
        }

        // ============================================
        // حذف مستند
        // ============================================
        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.SiteDocuments.FindAsync(id);
            if (document == null)
                return NotFound();

            var siteId = document.SiteId;
            var fileName = document.FileName;

            _fileUpload.DeleteFile(document.FilePath);
            _context.SiteDocuments.Remove(document);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(SiteDocument), id.ToString(), $"حذف مستند موقع: {fileName}");

            TempData["Success"] = "تم حذف المستند بنجاح";
            return RedirectToAction("Index", new { siteId });
        }
    }
}