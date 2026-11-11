using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AtharERP_System.Controllers
{
    public class MyCustodyController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly NotificationService _notify;
        private readonly FileUploadService _fileUpload;
        private readonly HRFormsPdfService _pdfService;
        private readonly IWebHostEnvironment _environment;

        public MyCustodyController(AppDbContext context, AuditService audit, NotificationService notify, FileUploadService fileUpload, HRFormsPdfService pdfService, IWebHostEnvironment environment)
        {
            _context = context;
            _audit = audit;
            _notify = notify;
            _fileUpload = fileUpload;
            _pdfService = pdfService;
            _environment = environment;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var handovers = await _context.CustodyHandovers
                .Include(h => h.CreatedBy)
                .Include(h => h.Lines).ThenInclude(l => l.Item).ThenInclude(i => i!.Category)
                .Where(h => h.HolderId == CurrentUserId)
                .OrderByDescending(h => h.HandedDate)
                .ToListAsync();

            var me = await _context.Users.FindAsync(CurrentUserId);
            ViewBag.MySignaturePath = me?.SignatureImagePath;

            return View(handovers);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var handover = await _context.CustodyHandovers.Include(h => h.Holder).FirstOrDefaultAsync(h => h.Id == id && h.HolderId == CurrentUserId);
            if (handover == null)
                return NotFound();

            if (handover.HolderConfirmed)
            {
                TempData["Error"] = "تم تأكيد استلام هذه العهدة مسبقاً";
                return RedirectToAction("Index");
            }

            handover.HolderConfirmed = true;
            handover.HolderConfirmedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await ArchiveCustodyPdfAsync(handover.Id);

            await _audit.LogAsync(CurrentUserId, "Update", nameof(CustodyHandover), handover.Id.ToString(), "تأكيد استلام عهدة");
            await _notify.NotifyAsync(handover.CreatedById, $"تم تأكيد استلام العهدة من قِبل {handover.Holder?.FullName}", NotificationEventType.CustodyHandoverConfirmed, "/Custody/Index", entityType: "CustodyHandover", entityId: handover.Id);

            TempData["Success"] = "تم تأكيد الاستلام";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var handover = await _context.CustodyHandovers.FirstOrDefaultAsync(h => h.Id == id && h.HolderId == CurrentUserId);
            if (handover == null || string.IsNullOrEmpty(handover.PdfFilePath))
                return NotFound();

            var fullPath = Path.Combine(_environment.WebRootPath, handover.PdfFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            return PhysicalFile(fullPath, "application/pdf", $"عهدة-{handover.Id}.pdf");
        }

        private async Task ArchiveCustodyPdfAsync(int handoverId)
        {
            var full = await _context.CustodyHandovers
                .Include(h => h.Holder).ThenInclude(u => u!.PositionRef)
                .Include(h => h.Holder).ThenInclude(u => u!.Department)
                .Include(h => h.CreatedBy).ThenInclude(u => u!.PositionRef)
                .Include(h => h.Lines).ThenInclude(l => l.Item)
                .FirstAsync(h => h.Id == handoverId);

            var bytes = _pdfService.GenerateCustodyHandoverPdf(full);
            if (!string.IsNullOrEmpty(full.PdfFilePath))
                _fileUpload.DeleteFile(full.PdfFilePath);

            var result = await _fileUpload.SaveGeneratedFileAsync(bytes, $"custody/{full.Id}", ".pdf");
            if (result.Success)
            {
                full.PdfFilePath = result.FilePath;
                await _context.SaveChangesAsync();
            }
        }
    }
}