using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ProjectStep
    {
        public int Id { get; set; }

        [Required]
        public int StageId { get; set; }

        [ForeignKey("StageId")]
        public virtual ProjectStage Stage { get; set; } = null!;

        [Required(ErrorMessage = "اسم الخطوة مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم الخطوة")]
        public string Name { get; set; } = string.Empty;

        // الوزن ثابت بعد الإنشاء (قاعدة عمل من الوحدة 1، لا تزال سارية)
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "الوزن (% داخل المرحلة)")]
        public decimal Weight { get; set; }

        [Display(Name = "الحالة")]
        public StepStatus Status { get; set; } = StepStatus.NotStarted;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "التكلفة الفعلية")]
        public decimal ActualCost { get; set; }

        [Display(Name = "تاريخ الإكمال")]
        public DateTime? CompletedDate { get; set; }

        [Display(Name = "أُكملت بواسطة")]
        public string? CompletedById { get; set; }

        [ForeignKey("CompletedById")]
        public virtual ApplicationUser? CompletedBy { get; set; }
    }
}