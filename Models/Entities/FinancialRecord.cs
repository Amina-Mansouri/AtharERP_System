using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    // كيان مبسّط تم تقديمه من الوحدة 05 (المالية) لاستقبال التكاليف المرحّلة تلقائياً
    // عند اكتمال ProjectAssignment، حسب القسم 5.7/6.5 من مواصفة الوحدة 02.
    public class FinancialRecord
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        public int? ProjectAssignmentId { get; set; }

        [ForeignKey("ProjectAssignmentId")]
        public virtual ProjectAssignment? ProjectAssignment { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "نوع التكلفة")]
        public string CostType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المساحة (م²)")]
        public decimal? Area { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "القيمة")]
        public decimal Value { get; set; }

        [Display(Name = "مُسدَّد")]
        public bool IsCleared { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}