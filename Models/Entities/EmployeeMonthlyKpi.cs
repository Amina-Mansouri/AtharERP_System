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

        [StringLength(100)]
        [Display(Name = "التخصص")]
        public string? Discipline { get; set; }

        [StringLength(100)]
        [Display(Name = "القسم")]
        public string? TeamDepartment { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "ساعات العمل المعيارية")]
        public decimal? StandardWorkingHours { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "ساعات الإجازة المعتمدة")]
        public decimal? ApprovedLeaveHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة استغلال ساعات العمل")]
        public decimal? WorkingHoursUtilizationPercent { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "هدف الإنتاج المعياري")]
        public decimal? StandardProductionTarget { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "هدف الإنتاج المعدَّل")]
        public decimal? AdjustedProductionTarget { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "إجمالي الإنتاج الأساسي")]
        public decimal? TotalBaseProduction { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "الإنتاج المعترف به بالفترة")]
        public decimal? RecognizedProductionWithinPeriod { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة تحقيق الإنتاج")]
        public decimal? ProductionAchievementPercent { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "إجمالي خصم النسخ")]
        public decimal? TotalRevisionDeduction { get; set; }

        [Display(Name = "تكليفات مبكرة")]
        public int? EarlyTasks { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة التسليم بالوقت")]
        public decimal? OnTimeDeliveryRatePercent { get; set; }

        [Display(Name = "إجمالي التكليفات")]
        public int? TotalTasksCount { get; set; }

        [Display(Name = "تكليفات مكتملة")]
        public int? CompletedTasks { get; set; }

        [Display(Name = "تكليفات معلّقة")]
        public int? PendingTasks { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "وزن KPI الهندسي")]
        public decimal? EngineeringKpiWeight { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "وزن تقييم الشؤون الإدارية")]
        public decimal? HrEvaluationWeight { get; set; }
    }
}
