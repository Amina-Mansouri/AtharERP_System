using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // صف "جدول البيع النهائي / المطالبة" — يُنشأ عند ترحيل التكليف إلى المالية
    public class FinancialClaim
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Required]
        public int ProjectAssignmentId { get; set; }

        [ForeignKey("ProjectAssignmentId")]
        [ValidateNever]
        public virtual ProjectAssignment ProjectAssignment { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Display(Name = "الرمز")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "البيان")]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المساحة (م²)")]
        public decimal? Area { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "سعر متر البيع")]
        public decimal? SalePricePerMeter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "قيمة البيع")]
        public decimal Value { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة 1")]
        public decimal? SaleMarkupPercent1 { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة 2")]
        public decimal? SaleMarkupPercent2 { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة زيادة إعادة التصميم")]
        public decimal? RedesignIncreasePercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المدفوع")]
        public decimal PaidAmount { get; set; }

        // القيمة الحقيقية المستحقة/المحصَّلة فعلياً من العميل = قيمة البيع + زيادة إعادة التصميم فقط (بلا تأثير نسبة1/نسبة2 الإعلاميتين)
        [NotMapped]
        public decimal RealValue => Value + RedesignResult;

        [NotMapped]
        [Display(Name = "المتبقي")]
        public decimal RemainingAmount => RealValue - PaidAmount;

        // ناتج نسبة 1 = قيمة البيع الأصلية × نسبة1 (مستقلة عن نسبة2، لا تراكمية)
        [NotMapped]
        public decimal MarkupResult1 => Value * (SaleMarkupPercent1 ?? 0) / 100;

        // ناتج نسبة 2 = قيمة البيع الأصلية × نسبة2 (مستقلة عن نسبة1)
        [NotMapped]
        public decimal MarkupResult2 => Value * (SaleMarkupPercent2 ?? 0) / 100;

        [NotMapped]
        public decimal RedesignResult => Value * (RedesignIncreasePercentage ?? 0) / 100;

        [NotMapped]
        [Display(Name = "قيمة البيع بعد النسبة")]
        public decimal ValueAfterPercentage => Value - MarkupResult1 - MarkupResult2 + RedesignResult;

        [Display(Name = "مرحّلة إلى المالية")]
        public bool IsTransferredToFinance { get; set; }

        [Display(Name = "تاريخ الترحيل")]
        public DateTime? TransferredToFinanceAt { get; set; }

        [Display(Name = "تم التحصيل")]
        public bool IsClientSettled { get; set; }

        [Display(Name = "تاريخ التحصيل")]
        public DateTime? ClientSettledAt { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public virtual ICollection<ClaimPayment> Payments { get; set; } = new List<ClaimPayment>();

        [Display(Name = "أُنشئت بواسطة")]
        [ValidateNever]
        public string? CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        [ValidateNever]
        public virtual ApplicationUser? CreatedBy { get; set; }
    }
}