using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    // تتحقق إن القيم التصنيفية بأي ملف (قالب مرجعي أو رفعة شهرية) تطابق تصنيفات نظامنا الحية —
    // لا تخزّن القوائم بنفسها، تقرأها من القسم/الـenum/جدول أنواع المشاريع مباشرة حتى تبقى محدّثة تلقائياً
    public class ClassificationValidationService
    {
        private readonly AppDbContext _context;

        public ClassificationValidationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> ValidateAsync(
            IEnumerable<string> disciplineValues,
            IEnumerable<string> priorityValues,
            IEnumerable<string> revisionReasonValues,
            IEnumerable<string> projectTypeValues)
        {
            var errors = new List<string>();

            var validDisciplines = await _context.Departments
                .Where(d => d.ParentDepartment!.IsStageDisciplinesParent && d.IsActive)
                .Select(d => d.Name)
                .ToListAsync();
            CheckValues(disciplineValues, validDisciplines, "التخصص", errors);

            var validPriorities = EnumDisplayHelper.GetDisplayList<TaskPriority>().Select(x => x.Label).ToList();
            CheckValues(priorityValues, validPriorities, "الأولوية", errors);

            var validRevisionReasons = EnumDisplayHelper.GetDisplayList<ProposalStatus>()
                .Where(x => x.Value == nameof(ProposalStatus.Resubmission) || x.Value == nameof(ProposalStatus.Redesign))
                .Select(x => x.Label).ToList();
            CheckValues(revisionReasonValues, validRevisionReasons, "سبب النسخة", errors);

            var validProjectTypes = await _context.ProjectCategories
                .Where(c => c.IsActive)
                .Select(c => c.Classification)
                .ToListAsync();
            CheckValues(projectTypeValues, validProjectTypes, "نوع المشروع", errors);

            return errors;
        }

        private static void CheckValues(IEnumerable<string> foundValues, List<string> validValues, string fieldLabel, List<string> errors)
        {
            var unknown = foundValues
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct()
                .Where(v => !validValues.Contains(v))
                .ToList();

            foreach (var v in unknown)
                errors.Add($"قيمة \"{v}\" بحقل \"{fieldLabel}\" غير معروفة بالنظام");
        }
    }
}