using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // رفعة الملف الشهري المكتمل (بعد تعبئته خارجياً وإدخال تقييم الشؤون الإدارية فيه) — تُحفظ وتُؤرشف هنا
    public class PerformanceEvaluationUpload
    {
        public int Id { get; set; }

        [Display(Name = "السنة")]
        public int Year { get; set; }

        [Display(Name = "الشهر")]
        public int Month { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "مسار الملف المؤرشف")]
        public string FilePath { get; set; } = string.Empty;

        [Display(Name = "اجتاز التحقق")]
        public bool ValidationPassed { get; set; }

        [Display(Name = "ملاحظات التحقق")]
        public string? ValidationNotes { get; set; }

        [Required]
        [ValidateNever]
        public string UploadedById { get; set; } = string.Empty;

        [ForeignKey("UploadedById")]
        [ValidateNever]
        public virtual ApplicationUser UploadedBy { get; set; } = null!;

        [Display(Name = "تاريخ الرفع")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<EmployeeMonthlyKpi> EmployeeResults { get; set; } = new List<EmployeeMonthlyKpi>();
    }
}