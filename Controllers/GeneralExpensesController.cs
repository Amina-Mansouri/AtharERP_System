using AtharERP_System.Authorization;
using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    public class GeneralExpensesController : Controller
    {
        private readonly AppDbContext _context;

        public GeneralExpensesController(AppDbContext context)
        {
            _context = context;
        }

        [RequirePermission("Finance.Costs.View")]
        public async Task<IActionResult> Index(int? categoryId, DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _context.ProjectExpenses
                .Include(e => e.ExpenseCategory)
                .Where(e => e.ProjectId == null);

            if (categoryId.HasValue)
                query = query.Where(e => e.ExpenseCategoryId == categoryId.Value);
            if (dateFrom.HasValue)
                query = query.Where(e => e.Date >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(e => e.Date <= dateTo.Value);

            var expenses = await query.OrderByDescending(e => e.Date).ToListAsync();

            ViewBag.CategoryId = categoryId;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.ExpenseCategories = await _context.ExpenseCategories
                .Where(c => c.IsActive && c.Scope == ExpenseCategoryScope.General)
                .OrderBy(c => c.NameAr)
                .ToListAsync();

            ViewBag.TotalAmount = expenses.Sum(e => e.Amount);
            ViewBag.CategoryBreakdown = expenses
                .GroupBy(e => e.ExpenseCategory!.NameAr)
                .Select(g => (Category: g.Key, Total: g.Sum(e => e.Amount)))
                .OrderByDescending(x => x.Total)
                .ToList();

            return View(expenses);
        }

        [RequirePermission("Finance.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ExpenseCategoryId,Amount,Date,Description")] ProjectExpense model)
        {
            model.ProjectId = null;
            model.CreatedAt = DateTime.UtcNow;
            _context.ProjectExpenses.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تسجيل المصروف الإداري بنجاح";
            return RedirectToAction("Index");
        }

        [RequirePermission("Finance.Costs.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _context.ProjectExpenses.FirstOrDefaultAsync(e => e.Id == id && e.ProjectId == null);
            if (expense == null)
                return NotFound();

            _context.ProjectExpenses.Remove(expense);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف المصروف بنجاح";
            return RedirectToAction("Index");
        }
    }
}