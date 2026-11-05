using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SalaryPayment
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser User { get; set; } = null!;

        [Display(Name = "السنة")]
        public int Year { get; set; }

        [Display(Name = "الشهر")]
        public int Month { get; set; }

        // ===== مصادر الأرقام (شفافية وتدقيق) =====
        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "ساعات العمل الفعلية")]
        public decimal WorkedHours { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "ساعات الإضافي المعتمدة")]
        public decimal OvertimeHoursApproved { get; set; }

        [Display(Name = "أيام الغياب التقديرية")]
        public int EstimatedAbsentDays { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "دقائق التأخير/الانصراف المبكر")]
        public decimal LateMinutesTotal { get; set; }

        // ===== لقطة الأسعار وقت الصرف (لحماية السجل التاريخي من أي تعديل لاحق على الرتبة/المنصب) =====
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "سعر الساعة المستخدم")]
        public decimal? HourlyRateUsed { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الراتب الثابت المستخدم")]
        public decimal? FixedSalaryUsed { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "نسبة المنصب المستخدمة")]
        public decimal PositionPercentageUsed { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "نسبة الإضافي المستخدمة")]
        public decimal OvertimePercentageUsed { get; set; }

        [StringLength(100)]
        [Display(Name = "المنصب وقت الصرف")]
        public string? PositionNameUsed { get; set; }

        [StringLength(100)]
        [Display(Name = "الرتبة وقت الصرف")]
        public string? RankNameUsed { get; set; }

        // ===== المبالغ النهائية =====
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الأساسي")]
        public decimal BaseAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الإضافي")]
        public decimal OvertimeAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الخصم")]
        public decimal Deduction { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الصافي")]
        public decimal NetAmount { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "تاريخ الصرف")]
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "صرفه")]
        public string PaidById { get; set; } = string.Empty;

        [ForeignKey("PaidById")]
        [ValidateNever]
        public virtual ApplicationUser PaidBy { get; set; } = null!;
    }
}