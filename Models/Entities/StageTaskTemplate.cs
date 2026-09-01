using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    // قالب مهام المرحلة (بند P1) — تابع لمرحلة مشروع فعلية (ProjectStage)، وليس قائمة عامة
    // مشتركة بين المشاريع؛ عند تفعيل المرحلة تُنسخ بنوده كمهام فعلية قابلة للتعديل بعد النسخ.
    public class StageTaskTemplate
    {
        public int Id { get; set; }

        [Required]
        public int ProjectStageId { get; set; }

        [ForeignKey("ProjectStageId")]
        public virtual ProjectStage ProjectStage { get; set; } = null!;

        [Required(ErrorMessage = "اسم المهمة مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم المهمة")]
        public string TaskName { get; set; } = string.Empty;

        [Display(Name = "الترتيب")]
        public int Order { get; set; }

        [Display(Name = "المدة المتوقعة (أيام)")]
        public int? ExpectedDays { get; set; }

        [Display(Name = "المخرَج المطلوب")]
        public TaskOutputType? OutputType { get; set; }
    }
}