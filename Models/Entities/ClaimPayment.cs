using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // سجل كل دفعة فعلية سُجِّلت على مطالبة، لدعم سجل الدفعات وطباعة الإيصالات
    public class ClaimPayment
    {
        public int Id { get; set; }

        [Required]
        public int FinancialClaimId { get; set; }

        [ForeignKey("FinancialClaimId")]
        [ValidateNever]
        public virtual FinancialClaim FinancialClaim { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        [Display(Name = "تاريخ الدفعة")]
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "سُجِّلت بواسطة")]
        [ValidateNever]
        public string? CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        [ValidateNever]
        public virtual ApplicationUser? CreatedBy { get; set; }
    }
}