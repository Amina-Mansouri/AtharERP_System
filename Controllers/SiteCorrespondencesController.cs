using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class SiteCorrespondencesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly FileUploadService _fileUpload;
        private readonly SiteCorrespondencePdfService _pdfService;
        private readonly NotificationService _notify;
        private readonly IWebHostEnvironment _environment;

        public SiteCorrespondencesController(AppDbContext context, PermissionService permissionService,
            FileUploadService fileUpload, SiteCorrespondencePdfService pdfService, NotificationService notify,
            IWebHostEnvironment environment)
        {
            _context = context;
            _permissionService = permissionService;
            _fileUpload = fileUpload;
            _pdfService = pdfService;
            _notify = notify;
            _environment = environment;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Index(int siteId)
        {
            var site = await _context.Sites.Include(s => s.Project).FirstOrDefaultAsync(s => s.Id == siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            var items = await _context.SiteCorrespondences
                .Include(r => r.SentBy)
                .Include(r => r.SentByContractor)
                .Where(r => r.SiteId == siteId)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            var unreadFromContractor = items.Where(r => r.IsFromContractor && !r.IsRead).ToList();
            foreach (var r in unreadFromContractor)
                r.IsRead = true;
            if (unreadFromContractor.Any())
                await _context.SaveChangesAsync();

            ViewBag.Site = site;
            return View(items);
        }

        [RequirePermission("Sites.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int siteId, int? parentCorrespondenceId, CorrespondencePriority priority,
    DateTime? requiredResponseDate, string detailedQuery, List<IFormFile>? attachments)
        {
            var site = await _context.Sites.Include(s => s.Project).FirstOrDefaultAsync(s => s.Id == siteId);
            if (site == null)
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, site.ProjectId))
                return Forbid();

            var user = await _context.Users.Include(u => u.JobRankRef).FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            var attachmentEntities = new List<SiteCorrespondenceAttachment>();
            if (attachments != null)
            {
                foreach (var file in attachments.Where(f => f.Length > 0))
                {
                    var result = await _fileUpload.SaveFileUnrestrictedAsync(file, $"sites/{siteId}/correspondences");
                    if (result.Success)
                        attachmentEntities.Add(new SiteCorrespondenceAttachment { FilePath = result.FilePath, FileName = file.FileName });
                }
            }

            var nextRequestNumber = (await _context.SiteCorrespondences.MaxAsync(r => (int?)r.RequestNumber) ?? 0) + 1;

            var item = new SiteCorrespondence
            {
                SiteId = siteId,
                RequestNumber = nextRequestNumber,
                Priority = priority,
                RequiredResponseDate = requiredResponseDate,
                DetailedQuery = detailedQuery,
                Attachments = attachmentEntities,
                IsFromContractor = false,
                SubmittedByName = user?.FullName ?? "-",
                SubmittedByPosition = user?.JobRankRef?.NameAr,
                SubmittedBySignaturePath = user?.SignatureImagePath,
                ApproverName = user?.FullName ?? "-",
                ApproverPosition = user?.JobRankRef?.NameAr,
                ApproverSignaturePath = user?.SignatureImagePath,
                SentById = CurrentUserId,
                ParentCorrespondenceId = parentCorrespondenceId,
                Status = CorrespondenceStatus.Pending,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var pdfBytes = _pdfService.Generate(item, site);
            var pdfResult = await _fileUpload.SaveGeneratedFileAsync(pdfBytes, $"correspondences/site-{siteId}", ".pdf");
            if (pdfResult.Success)
                item.PdfFilePath = pdfResult.FilePath;

            _context.SiteCorrespondences.Add(item);

            if (parentCorrespondenceId.HasValue)
            {
                var parent = await _context.SiteCorrespondences.FindAsync(parentCorrespondenceId.Value);
                if (parent != null)
                    parent.Status = CorrespondenceStatus.Resolved;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إرسال المراسلة للمقاول بنجاح";
            return RedirectToAction("Index", new { siteId });
        }

        [RequirePermission("Sites.View")]
        public async Task<IActionResult> Download(int id)
        {
            var item = await _context.SiteCorrespondences.Include(r => r.Site).Include(r => r.Attachments).FirstOrDefaultAsync(r => r.Id == id);
            if (item == null || string.IsNullOrEmpty(item.PdfFilePath))
                return NotFound();

            if (!await _permissionService.CanAccessProjectAsync(User, item.Site.ProjectId))
                return Forbid();

            var pdfFullPath = Path.Combine(_environment.WebRootPath, item.PdfFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(pdfFullPath))
                return NotFound();

            var reportName = $"مراسلة-{item.Site.Code}-{item.RequestNumber:D4}";

            if (!item.Attachments.Any())
                return PhysicalFile(pdfFullPath, "application/pdf", $"{reportName}.pdf");

            using var memoryStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                archive.CreateEntryFromFile(pdfFullPath, $"{reportName}.pdf");
                foreach (var att in item.Attachments)
                {
                    var attFullPath = Path.Combine(_environment.WebRootPath, att.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(attFullPath))
                        archive.CreateEntryFromFile(attFullPath, att.FileName);
                }
            }
            return File(memoryStream.ToArray(), "application/zip", $"{reportName}.zip");
        }
    }
}