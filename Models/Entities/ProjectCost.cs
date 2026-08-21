using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class ProjectCost
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Required(ErrorMessage = "نوع التكلفة مطلوب")]
        [StringLength(100)]
        [Display(Name = "نوع التكلفة")]
        public string CostType { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المساحة (م²)")]
        public decimal? Area { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "السعر لكل متر")]
        public decimal? PricePerMeter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الخصم/الإضافة")]
        public decimal DiscountOrAdditionPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المبلغ النهائي")]
        public decimal FinalAmount { get; set; }

        [Display(Name = "الحالة")]
        public CostStatus Status { get; set; } = CostStatus.Pending;

        [Display(Name = "مرحّل إلى المالية")]
        public bool IsTransferredToFinance { get; set; }

        [Display(Name = "تاريخ الترحيل")]
        public DateTime? TransferredToFinanceAt { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ProjectCostSubtask> Subtasks { get; set; } = new List<ProjectCostSubtask>();
    }
}