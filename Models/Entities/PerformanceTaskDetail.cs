using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // صف واحد لكل تكليف + نسخة (إصدار) — نسخة كاملة من شيت "kpi calculations" لشهر واحد
    public class PerformanceTaskDetail
    {
        public int Id { get; set; }

        [Required]
        public int PerformanceEvaluationUploadId { get; set; }

        [ForeignKey("PerformanceEvaluationUploadId")]
        [ValidateNever]
        public virtual PerformanceEvaluationUpload Upload { get; set; } = null!;

        [Required]
        [ValidateNever]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser User { get; set; } = null!;

        [StringLength(50)]
        [Display(Name = "رقم المشروع")]
        public string? ProjectNumber { get; set; }

        [StringLength(255)]
        [Display(Name = "اسم المشروع")]
        public string? ProjectName { get; set; }

        [StringLength(100)]
        [Display(Name = "نوع المشروع")]
        public string? ProjectType { get; set; }

        [StringLength(100)]
        [Display(Name = "التخصص")]
        public string? Discipline { get; set; }

        [StringLength(50)]
        [Display(Name = "رقم المبنى")]
        public string? BuildingNumber { get; set; }

        [StringLength(255)]
        [Display(Name = "اسم المبنى")]
        public string? BuildingName { get; set; }

        [StringLength(50)]
        [Display(Name = "الأولوية")]
        public string? Priority { get; set; }

        [StringLength(50)]
        [Display(Name = "حالة المهمة")]
        public string? TaskStatus { get; set; }

        [StringLength(100)]
        [Display(Name = "النطاق")]
        public string? Scope { get; set; }

        [StringLength(100)]
        [Display(Name = "العنصر")]
        public string? Element { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "المساحة")]
        public decimal? BuildingArea { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "معامل نوع المشروع")]
        public decimal? ProjectTypeFactor { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "وزن النطاق")]
        public decimal? ScopeWeight { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "وزن العنصر")]
        public decimal? ElementWeight { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "الإنتاج الأساسي")]
        public decimal? BaseProduction { get; set; }

        [StringLength(20)]
        [Display(Name = "حالة الموعد")]
        public string? DeadlineStatus { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "عامل الموعد")]
        public decimal? DeadlineFactor { get; set; }

        [Display(Name = "عدد النسخ")]
        public int? RevisionCount { get; set; }

        [StringLength(50)]
        [Display(Name = "سبب النسخة")]
        public string? RevisionReason { get; set; }

        [Display(Name = "نسخ مؤهَّلة")]
        public int? EligibleRevisions { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "خصم الأهلية")]
        public decimal? EligibilityReduction { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "امتثال النسخ")]
        public decimal? RevisionCompliance { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "الدرجة النهائية للمهمة")]
        public decimal? FinalTaskScore { get; set; }

        [Display(Name = "تاريخ بداية المهمة")]
        public DateTime? TaskStartDate { get; set; }

        [Display(Name = "تاريخ استحقاق المهمة")]
        public DateTime? TaskDueDate { get; set; }

        [Display(Name = "إجمالي أيام المهمة")]
        public int? TotalTaskDays { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "الإنتاج اليومي الأساسي")]
        public decimal? DailyBaseProduction { get; set; }

        [Display(Name = "أيام ضمن فترة التقرير")]
        public int? DaysWithinReportPeriod { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "الإنتاج المعترف به بالفترة")]
        public decimal? RecognizedProductionWithinPeriod { get; set; }
    }
}