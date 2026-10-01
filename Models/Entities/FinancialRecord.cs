using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // صف "جدول التكاليف" — يُنشأ عند ترحيل التكليف إلى المالية، لكل مهندسة ضمنه صف مستقل
    public class FinancialRecord
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
        [Display(Name = "المهندسة")]
        public string EngineerId { get; set; } = string.Empty;

        [ForeignKey("EngineerId")]
        [ValidateNever]
        public virtual ApplicationUser Engineer { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المساحة (م²)")]
        public decimal? Area { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "سعر متر التكليف")]
        public decimal? PricePerMeter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "قيمة التكليف")]
        public decimal Value { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة مساهمة")]
        public decimal? ContributionPercentage { get; set; }


        [NotMapped]
        [Display(Name = "قيمة التكليف بعد النسبة")]
        public decimal ValueAfterPercentage => Value * (ContributionPercentage ?? 0) / 100;

        [Display(Name = "تم الصرف")]
        public bool IsCleared { get; set; }

        [Display(Name = "تاريخ الصرف")]
        public DateTime? ClearedAt { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}