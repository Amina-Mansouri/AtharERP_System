using AtharERP_System.Authorization;
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
    public class CustodyController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly AuditService _audit;
        private readonly NotificationService _notify;
        private readonly FileUploadService _fileUpload;
        private readonly HRFormsPdfService _pdfService;
        private readonly IWebHostEnvironment _environment;

        public CustodyController(AppDbContext context, PermissionService permissionService, AuditService audit, NotificationService notify, FileUploadService fileUpload, HRFormsPdfService pdfService, IWebHostEnvironment environment)
        {
            _context = context;
            _permissionService = permissionService;
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
            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Custody.Manage");
            ViewBag.CanManage = canManage;

            var myHandovers = await _context.CustodyHandovers
                .Include(h => h.CreatedBy)
                .Include(h => h.Lines).ThenInclude(l => l.Item).ThenInclude(i => i!.Category)
                .Where(h => h.HolderId == CurrentUserId)
                .OrderByDescending(h => h.HandedDate)
                .ToListAsync();
            var me = await _context.Users.FindAsync(CurrentUserId);
            ViewBag.MySignaturePath = me?.SignatureImagePath;

            if (canManage)
            {
                ViewBag.Categories = await _context.CustodyCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
                ViewBag.Items = await _context.CustodyItems.Include(i => i.Category).Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
                ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();

                ViewBag.AllHandovers = await _context.CustodyHandovers
                    .Include(h => h.Holder)
                    .Include(h => h.CreatedBy)
                    .Include(h => h.Lines).ThenInclude(l => l.Item)
                    .OrderByDescending(h => h.HandedDate)
                    .ToListAsync();
            }

            return View(myHandovers);
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

        [RequirePermission("HR.Custody.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "اسم التصنيف مطلوب";
                return RedirectToAction("Index");
            }

            _context.CustodyCategories.Add(new CustodyCategory { Name = name.Trim() });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة التصنيف";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Custody.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateItem(string code, string name, int categoryId, int totalQuantity)
        {
            if (totalQuantity <= 0)
            {
                TempData["Error"] = "الكمية الإجمالية يجب أن تكون أكبر من صفر";
                return RedirectToAction("Index");
            }

            var item = new CustodyItem
            {
                Code = code,
                Name = name,
                CategoryId = categoryId,
                TotalQuantity = totalQuantity,
                AvailableQuantity = totalQuantity
            };
            _context.CustodyItems.Add(item);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Create", nameof(CustodyItem), item.Id.ToString(), $"إضافة مادة عهدة \"{name}\" بكمية {totalQuantity}");

            TempData["Success"] = "تم إضافة المادة لكتالوج العهد";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Custody.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHandover(string holderId, DateTime? handedDate, List<int?> itemIds, List<int> quantities)
        {
            if (string.IsNullOrEmpty(holderId))
            {
                TempData["Error"] = "يجب اختيار الموظف";
                return RedirectToAction("Index");
            }

            var pairs = new List<(int ItemId, int Quantity)>();
            for (int i = 0; i < (itemIds?.Count ?? 0); i++)
            {
                if (itemIds![i].HasValue && i < quantities.Count && quantities[i] > 0)
                    pairs.Add((itemIds[i]!.Value, quantities[i]));
            }

            if (!pairs.Any())
            {
                TempData["Error"] = "يجب إضافة مادة واحدة على الأقل بكمية صحيحة";
                return RedirectToAction("Index");
            }

            var itemIdsNeeded = pairs.Select(p => p.ItemId).Distinct().ToList();
            var items = await _context.CustodyItems.Where(i => itemIdsNeeded.Contains(i.Id)).ToListAsync();

            foreach (var (itemId, qty) in pairs)
            {
                var item = items.First(i => i.Id == itemId);
                if (qty > item.AvailableQuantity)
                {
                    TempData["Error"] = $"الكمية المتاحة من \"{item.Name}\" هي {item.AvailableQuantity} فقط";
                    return RedirectToAction("Index");
                }
            }

            var handover = new CustodyHandover
            {
                HolderId = holderId,
                HandedDate = (handedDate ?? DateTime.UtcNow).Date,
                CreatedById = CurrentUserId
            };

            foreach (var (itemId, qty) in pairs)
            {
                var item = items.First(i => i.Id == itemId);
                item.AvailableQuantity -= qty;
                handover.Lines.Add(new CustodyHandoverLine
                {
                    CustodyItemId = itemId,
                    QuantityHanded = qty
                });
            }

            _context.CustodyHandovers.Add(handover);
            await _context.SaveChangesAsync();
            await ArchiveCustodyPdfAsync(handover.Id);

            await _audit.LogAsync(CurrentUserId, "Create", nameof(CustodyHandover), handover.Id.ToString(), "تسليم عهدة جديدة لموظف");
            await _notify.NotifyAsync(holderId, "تم تسليمك عهدة جديدة — راجعي شاشة \"العهد\" لتأكيد الاستلام", NotificationEventType.CustodyHandoverCreated, "/Custody/Index", requiresAction: true, entityType: "CustodyHandover", entityId: handover.Id);

            TempData["Success"] = "تم تسجيل تسليم العهدة بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("HR.Custody.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnLine(int lineId, int quantity)
        {
            var line = await _context.CustodyHandoverLines.Include(l => l.Item).FirstOrDefaultAsync(l => l.Id == lineId);
            if (line == null)
                return NotFound();

            var outstanding = line.QuantityHanded - line.QuantityReturned;
            if (quantity <= 0 || quantity > outstanding)
            {
                TempData["Error"] = $"الكمية غير صحيحة — المتبقي بحوزة الموظف {outstanding}";
                return RedirectToAction("Index");
            }

            line.QuantityReturned += quantity;
            line.Status = line.QuantityReturned >= line.QuantityHanded ? CustodyLineStatus.Returned : CustodyLineStatus.PartiallyReturned;

            if (line.Item != null)
                line.Item.AvailableQuantity += quantity;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Update", nameof(CustodyHandoverLine), line.Id.ToString(), $"استرجاع {quantity} من عهدة");

            await _audit.LogAsync(CurrentUserId, "Update", nameof(CustodyHandoverLine), line.Id.ToString(), $"استرجاع {quantity} من عهدة");

            TempData["Success"] = "تم تسجيل الاسترجاع";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var handover = await _context.CustodyHandovers.FirstOrDefaultAsync(h => h.Id == id);
            if (handover == null || string.IsNullOrEmpty(handover.PdfFilePath))
                return NotFound();

            bool canManage = await _permissionService.HasPermissionAsync(User, "HR.Custody.Manage");
            if (handover.HolderId != CurrentUserId && !canManage)
                return Forbid();

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