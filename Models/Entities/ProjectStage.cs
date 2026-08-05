using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ProjectStage
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [Required(ErrorMessage = "اسم المرحلة مطلوب")]
        [StringLength(200)]
        [Display(Name = "اسم المرحلة")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "الترتيب")]
        public int Order { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "الوزن (% من المشروع)")]
        public decimal Weight { get; set; }

        [Display(Name = "الحالة")]
        public ProjectStatus Status { get; set; } = ProjectStatus.New;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "التكلفة")]
        public decimal Cost { get; set; }

        [Display(Name = "المهندس المسؤول")]
        public string? AssignedEngineerId { get; set; }

        [ForeignKey("AssignedEngineerId")]
        public virtual ApplicationUser? AssignedEngineer { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الإنجاز")]
        public decimal CompletionPercentage { get; set; }

        public virtual ICollection<ProjectStep> Steps { get; set; } = new List<ProjectStep>();
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    }
}