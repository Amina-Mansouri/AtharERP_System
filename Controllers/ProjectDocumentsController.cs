using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class ProjectDocumentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly FileUploadService _fileUpload;
        private readonly AuditService _audit;

        public ProjectDocumentsController(
            AppDbContext context,
            PermissionService permissionService,
            FileUploadService fileUpload,
            AuditService audit)
        {
            _context = context;
            _permissionService = permissionService;
            _fileUpload = fileUpload;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int projectId, IFormFile file, string? description)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                return NotFound();

            var result = await _fileUpload.SaveFileAsync(file, $"projects/{projectId}");
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Details", "Projects", new { id = projectId });
            }

            var document = new ProjectDocument
            {
                ProjectId = projectId,
                FileName = file.FileName,
                FilePath = result.FilePath!,
                FileType = result.FileType,
                FileSize = result.FileSize,
                Description = description,
                UploadedAt = DateTime.UtcNow,
                UploadedById = CurrentUserId
            };

            _context.ProjectDocuments.Add(document);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Upload", nameof(ProjectDocument), document.Id.ToString(), $"رفع مستند: {document.FileName}");

            TempData["Success"] = "تم رفع المستند بنجاح";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        [RequirePermission("Projects.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.ProjectDocuments.FindAsync(id);
            if (document == null)
                return NotFound();

            var projectId = document.ProjectId;
            var fileName = document.FileName;

            _fileUpload.DeleteFile(document.FilePath);
            _context.ProjectDocuments.Remove(document);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Delete", nameof(ProjectDocument), id.ToString(), $"حذف مستند: {fileName}");

            TempData["Success"] = "تم حذف المستند بنجاح";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }
    }
}