using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SalaryAdvanceRequest
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser User { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        [Display(Name = "مدة السداد (أشهر)")]
        public int InstallmentMonths { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "القسط الشهري")]
        public decimal MonthlyInstallment { get; set; }

        [Display(Name = "أشهر مسدَّدة")]
        public int MonthsRepaid { get; set; }

        [Display(Name = "الحالة")]
        public SalaryAdvanceStatus Status { get; set; } = SalaryAdvanceStatus.Pending;

        [Display(Name = "تاريخ الطلب")]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public string? DecidedById { get; set; }

        [ForeignKey("DecidedById")]
        [ValidateNever]
        public virtual ApplicationUser? DecidedBy { get; set; }

        public DateTime? DecidedAt { get; set; }
    }
}