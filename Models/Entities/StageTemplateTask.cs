using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class StageTemplateTask
    {
        public int Id { get; set; }

        [Required]
        public int StageTemplateId { get; set; }

        [ForeignKey("StageTemplateId")]
        public virtual StageTemplate StageTemplate { get; set; } = null!;

        [Required(ErrorMessage = "اسم المهمة مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم المهمة")]
        public string TaskName { get; set; } = string.Empty;

        [Display(Name = "الترتيب")]
        public int Order { get; set; }
    }
}