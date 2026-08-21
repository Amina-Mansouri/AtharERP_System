using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ProjectCostSubtask
    {
        public int Id { get; set; }

        [Required]
        public int ProjectCostId { get; set; }

        [ForeignKey("ProjectCostId")]
        public virtual ProjectCost ProjectCost { get; set; } = null!;

        [Required(ErrorMessage = "اسم المهمة الفرعية مطلوب")]
        [StringLength(255)]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "مكتملة")]
        public bool IsCompleted { get; set; }

        [Display(Name = "تاريخ الإكمال")]
        public DateTime? CompletedAt { get; set; }
    }
}