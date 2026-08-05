using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ProjectStep
    {
        public int Id { get; set; }

        [Required]
        public int ProjectStageId { get; set; }

        [ForeignKey("ProjectStageId")]
        public virtual ProjectStage ProjectStage { get; set; } = null!;

        [Required(ErrorMessage = "اسم الخطوة مطلوب")]
        [StringLength(200)]
        [Display(Name = "اسم الخطوة")]
        public string Name { get; set; } = string.Empty;

        // الوزن ثابت بعد الإنشاء ولا يُعدَّل لاحقاً (قاعدة عمل)
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "الوزن (% داخل المرحلة)")]
        public decimal Weight { get; set; }

        [Display(Name = "الحالة")]
        public ProjectStatus Status { get; set; } = ProjectStatus.New;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "التكلفة الفعلية")]
        public decimal ActualCost { get; set; }
    }
}