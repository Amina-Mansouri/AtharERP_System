using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    // كتالوج أنواع المراحل الثابتة القابل للتعديل من الإدارة (بند P1)
    public class StageTemplate
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم القالب مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم المرحلة")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "الترتيب")]
        public int Order { get; set; }

        public virtual ICollection<StageTemplateTask> DefaultTasks { get; set; } = new List<StageTemplateTask>();
    }
}