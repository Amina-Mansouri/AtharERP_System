// Models/Entities/QualityCheckResult.cs
using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    // اسمها "QualityResult" في المواصفة، وأُضيفت لاحقة "Check" لتفادي التعارض مع أي enum نتيجة عام مستقبلاً
    public enum QualityCheckResult
    {
        [Display(Name = "معلق")] Pending = 1,
        [Display(Name = "مطابق")] Pass = 2,
        [Display(Name = "غير مطابق")] Fail = 3,
        [Display(Name = "يحتاج مراجعة")] NeedsReview = 4
    }
}