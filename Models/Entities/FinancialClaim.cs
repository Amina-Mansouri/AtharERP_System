using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // المطالبة المالية (وثيقة ١٣-٨ · Z5) — لا تُرسَل للمالية قبل اعتماد الإدارة الفنية
    // ثم اعتماد العميل (تحقّق في الـController عند بناء الشاشة، لا في الكيان).
    public class FinancialClaim
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Display(Name = "الرمز")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "البيان")]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "القيمة")]
        public decimal Value { get; set; }

        [Display(Name = "اعتماد الإدارة الفنية")]
        public DateTime? TechnicalApprovedAt { get; set; }

        [Display(Name = "اعتماد العميل")]
        public DateTime? ClientApprovedAt { get; set; }

        [Display(Name = "الحالة")]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [Display(Name = "مرحّلة إلى المالية")]
        public bool IsTransferredToFinance { get; set; }

        [Display(Name = "تاريخ الترحيل")]
        public DateTime? TransferredToFinanceAt { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "أُنشئت بواسطة")]
        [ValidateNever]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        [ValidateNever]
        public virtual ApplicationUser CreatedBy { get; set; } = null!;
    }
}