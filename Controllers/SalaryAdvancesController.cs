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
    public class SalaryAdvancesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notify;
        private readonly PermissionService _permissionService;
        private readonly FileUploadService _fileUpload;
        private readonly HRFormsPdfService _pdfService;
        private readonly IWebHostEnvironment _environment;

        public SalaryAdvancesController(AppDbContext context, NotificationService notify, PermissionService permissionService, FileUploadService fileUpload, HRFormsPdfService pdfService, IWebHostEnvironment environment)
        {
            _context = context;
            _notify = notify;
            _permissionService = permissionService;
            _fileUpload = fileUpload;
            _pdfService = pdfService;
            _environment = environment;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task ArchivePdfAsync(int requestId)
        {
            var full = await _context.SalaryAdvanceRequests
                .Include(r => r.User).ThenInclude(u => u!.PositionRef)
                .Include(r => r.User).ThenInclude(u => u!.Department)
                .Include(r => r.DecidedBy).ThenInclude(u => u!.PositionRef)
                .Include(r => r.GmDecidedBy).ThenInclude(u => u!.PositionRef)
                .FirstAsync(r => r.Id == requestId);

            var bytes = _pdfService.GenerateSalaryAdvancePdf(full);
            if (!string.IsNullOrEmpty(full.PdfFilePath))
                _fileUpload.DeleteFile(full.PdfFilePath);

            var result = await _fileUpload.SaveGeneratedFileAsync(bytes, $"salary-advances/{full.Id}", ".pdf");
            if (result.Success)
            {
                full.PdfFilePath = result.FilePath;
                await _context.SaveChangesAsync();
            }
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var myRequests = await _context.SalaryAdvanceRequests
                .Where(a => a.UserId == CurrentUserId)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync();

            var user = await _context.Users.FindAsync(CurrentUserId);
            ViewBag.ContractSalary = user?.ContractSalary ?? 0;
            ViewBag.HasActiveAdvance = myRequests.Any(a => a.Status == SalaryAdvanceStatus.Pending || a.Status == SalaryAdvanceStatus.PendingGeneralManager || (a.Status == SalaryAdvanceStatus.Approved && a.MonthsRepaid < a.InstallmentMonths));

            return View(myRequests);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(decimal amount, int installmentMonths)
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null)
                return NotFound();

            var hasActive = await _context.SalaryAdvanceRequests
                .AnyAsync(a => a.UserId == CurrentUserId && (a.Status == SalaryAdvanceStatus.Pending || a.Status == SalaryAdvanceStatus.PendingGeneralManager || (a.Status == SalaryAdvanceStatus.Approved && a.MonthsRepaid < a.InstallmentMonths)));
            if (hasActive)
            {
                TempData["Error"] = "لديكِ طلب سلفة قائم بالفعل (قيد الاعتماد أو لم تُسدَّد بالكامل) — لا يمكن تقديم طلب جديد";
                return RedirectToAction("Index");
            }

            if (installmentMonths < 1 || installmentMonths > 3)
            {
                TempData["Error"] = "مدة السداد يجب أن تكون بين شهر و3 أشهر";
                return RedirectToAction("Index");
            }

            var maxAmount = user.ContractSalary * 0.5m;
            if (amount <= 0 || amount > maxAmount)
            {
                TempData["Error"] = $"الحد الأقصى للسلفة هو {maxAmount:N0} د.ل (50% من الراتب)";
                return RedirectToAction("Index");
            }

            var monthlyInstallment = Math.Round(amount / installmentMonths, 2);
            var maxInstallment = user.ContractSalary * 0.3m;
            if (monthlyInstallment > maxInstallment)
            {
                TempData["Error"] = $"القسط الشهري ({monthlyInstallment:N0} د.ل) يتجاوز الحد المسموح (30% من الراتب = {maxInstallment:N0} د.ل) — اختاري مدة سداد أطول";
                return RedirectToAction("Index");
            }

            var request = new SalaryAdvanceRequest
            {
                UserId = CurrentUserId,
                Amount = amount,
                InstallmentMonths = installmentMonths,
                MonthlyInstallment = monthlyInstallment,
                MonthsRepaid = 0,
                Status = SalaryAdvanceStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            _context.SalaryAdvanceRequests.Add(request);
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            var approverIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("Finance.SalaryAdvances.Manage");
            await _notify.NotifyManyAsync(approverIds, $"طلب سلفة جديد من {user.FullName} بقيمة {amount:N0} د.ل", NotificationEventType.SalaryAdvanceRequestCreated, "/SalaryAdvances/Manage", requiresAction: true, entityType: "SalaryAdvanceRequest", entityId: request.Id);

            TempData["Success"] = "تم إرسال طلب السلفة بنجاح — بانتظار الاعتماد";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> Manage()
        {
            bool canManage = await _permissionService.HasPermissionAsync(User, "Finance.SalaryAdvances.Manage");
            bool canFinalApprove = await _permissionService.HasPermissionAsync(User, "Finance.SalaryAdvances.FinalApprove");
            if (!canManage && !canFinalApprove)
                return Forbid();

            ViewBag.CanManage = canManage;
            ViewBag.CanFinalApprove = canFinalApprove;

            var requests = await _context.SalaryAdvanceRequests
                .Include(a => a.User)
                .Include(a => a.DecidedBy)
                .Include(a => a.GmDecidedBy)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        [RequirePermission("Finance.SalaryAdvances.Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int id, bool approved)
        {
            var request = await _context.SalaryAdvanceRequests.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
            if (request == null || request.Status != SalaryAdvanceStatus.Pending)
                return NotFound();

            request.DecidedById = CurrentUserId;
            request.DecidedAt = DateTime.UtcNow;

            if (!approved)
            {
                request.Status = SalaryAdvanceStatus.Rejected;
                await _context.SaveChangesAsync();
                await ArchivePdfAsync(request.Id);
                await _notify.NotifyAsync(request.UserId, "تم رفض طلب سلفتك", NotificationEventType.SalaryAdvanceRequestDecided, "/SalaryAdvances/Index", entityType: "SalaryAdvanceRequest", entityId: request.Id);
                TempData["Success"] = "تم رفض الطلب";
                return RedirectToAction("Manage");
            }

            request.Status = SalaryAdvanceStatus.PendingGeneralManager;
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            var gmIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("Finance.SalaryAdvances.FinalApprove");
            await _notify.NotifyManyAsync(gmIds, $"طلب سلفة من {request.User?.FullName} بانتظار اعتمادك النهائي", NotificationEventType.SalaryAdvanceRequestCreated, "/SalaryAdvances/Manage", requiresAction: true, entityType: "SalaryAdvanceRequest", entityId: request.Id);
            await _notify.NotifyAsync(request.UserId, "تم اعتماد طلب سلفتك من المسؤول المالي، بانتظار اعتماد المدير العام", NotificationEventType.SalaryAdvanceRequestDecided, "/SalaryAdvances/Index", entityType: "SalaryAdvanceRequest", entityId: request.Id);

            TempData["Success"] = "تم اعتماد الطلب من المسؤول المالي، بانتظار اعتماد المدير العام";
            return RedirectToAction("Manage");
        }

        [RequirePermission("Finance.SalaryAdvances.FinalApprove")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalDecide(int id, bool approved)
        {
            var request = await _context.SalaryAdvanceRequests.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
            if (request == null)
                return NotFound();

            if (request.Status != SalaryAdvanceStatus.PendingGeneralManager)
            {
                TempData["Error"] = "هذا الطلب ليس بانتظار الاعتماد النهائي";
                return RedirectToAction("Manage");
            }

            request.Status = approved ? SalaryAdvanceStatus.Approved : SalaryAdvanceStatus.Rejected;
            request.GmDecidedById = CurrentUserId;
            request.GmDecidedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await ArchivePdfAsync(request.Id);

            await _notify.NotifyAsync(request.UserId, approved ? $"تم اعتماد طلب سلفتك نهائياً بقيمة {request.Amount:N0} د.ل" : "تم رفض طلب سلفتك من المدير العام", NotificationEventType.SalaryAdvanceRequestDecided, "/SalaryAdvances/Index", entityType: "SalaryAdvanceRequest", entityId: request.Id);

            TempData["Success"] = approved ? "تم الاعتماد النهائي للطلب" : "تم رفض الطلب";
            return RedirectToAction("Manage");
        }

        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var request = await _context.SalaryAdvanceRequests.FirstOrDefaultAsync(a => a.Id == id);
            if (request == null || string.IsNullOrEmpty(request.PdfFilePath))
                return NotFound();

            bool canManage = await _permissionService.HasPermissionAsync(User, "Finance.SalaryAdvances.Manage");
            bool canFinalApprove = await _permissionService.HasPermissionAsync(User, "Finance.SalaryAdvances.FinalApprove");
            if (request.UserId != CurrentUserId && !canManage && !canFinalApprove)
                return Forbid();

            var fullPath = Path.Combine(_environment.WebRootPath, request.PdfFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            return PhysicalFile(fullPath, "application/pdf", $"طلب-سلفة-{request.Id}.pdf");
        }
    }
}