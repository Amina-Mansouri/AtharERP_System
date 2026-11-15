using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // نتيجة موظفة واحدة لشهر واحد — مستخرجة من شيت Employee KPI Summary بالملف المرفوع
    public class EmployeeMonthlyKpi
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

        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "ساعات العمل المتاحة")]
        public decimal? AvailableWorkingHours { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "ساعات العمل الفعلية")]
        public decimal? ActualWorkingHours { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "إجمالي درجة الإنتاج النهائية")]
        public decimal? TotalFinalProductionScore { get; set; }

        [Display(Name = "إجمالي التكليفات")]
        public int? TotalTasks { get; set; }

        [Display(Name = "تكليفات في الوقت")]
        public int? OnTimeTasks { get; set; }

        [Display(Name = "تكليفات متأخرة")]
        public int? LateTasks { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "KPI الهندسي")]
        public decimal? EngineeringKpi { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "تقييم الشؤون الإدارية")]
        public decimal? HrEvaluation { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "KPI الشهري النهائي")]
        public decimal? FinalMonthlyKpi { get; set; }

        [StringLength(50)]
        [Display(Name = "تقدير الأداء")]
        public string? PerformanceGrade { get; set; }
    }
}