using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
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

        public SalaryAdvancesController(AppDbContext context, NotificationService notify, PermissionService permissionService)
        {
            _context = context;
            _notify = notify;
            _permissionService = permissionService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var myRequests = await _context.SalaryAdvanceRequests
                .Where(a => a.UserId == CurrentUserId)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync();

            var user = await _context.Users.FindAsync(CurrentUserId);
            ViewBag.ContractSalary = user?.ContractSalary ?? 0;
            ViewBag.HasActiveAdvance = myRequests.Any(a => a.Status == SalaryAdvanceStatus.Pending || (a.Status == SalaryAdvanceStatus.Approved && a.MonthsRepaid < a.InstallmentMonths));

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
                .AnyAsync(a => a.UserId == CurrentUserId && (a.Status == SalaryAdvanceStatus.Pending || (a.Status == SalaryAdvanceStatus.Approved && a.MonthsRepaid < a.InstallmentMonths)));
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

            var approverIds = await _permissionService.GetUsersWithPermissionOrAdminAsync("Finance.SalaryAdvances.Manage");
            await _notify.NotifyManyAsync(approverIds, $"طلب سلفة جديد من {user.FullName} بقيمة {amount:N0} د.ل", NotificationEventType.SalaryAdvanceRequestCreated, "/SalaryAdvances/Manage", requiresAction: true, entityType: "SalaryAdvanceRequest", entityId: request.Id);

            TempData["Success"] = "تم إرسال طلب السلفة بنجاح — بانتظار الاعتماد";
            return RedirectToAction("Index");
        }

        [RequirePermission("Finance.SalaryAdvances.Manage")]
        public async Task<IActionResult> Manage()
        {
            var requests = await _context.SalaryAdvanceRequests
                .Include(a => a.User)
                .Include(a => a.DecidedBy)
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

            request.Status = approved ? SalaryAdvanceStatus.Approved : SalaryAdvanceStatus.Rejected;
            request.DecidedById = CurrentUserId;
            request.DecidedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _notify.NotifyAsync(request.UserId, approved ? $"تم اعتماد طلب سلفتك بقيمة {request.Amount:N0} د.ل" : "تم رفض طلب سلفتك", NotificationEventType.SalaryAdvanceRequestDecided, "/SalaryAdvances/Index", entityType: "SalaryAdvanceRequest", entityId: request.Id);

            TempData["Success"] = approved ? "تم اعتماد الطلب" : "تم رفض الطلب";
            return RedirectToAction("Manage");
        }
    }
}