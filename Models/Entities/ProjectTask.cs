using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ProjectTask
    {
        public int Id { get; set; }

        [Required]
        public int ProjectStageId { get; set; }

        [ForeignKey("ProjectStageId")]
        public virtual ProjectStage ProjectStage { get; set; } = null!;

        [Required(ErrorMessage = "عنوان المهمة مطلوب")]
        [StringLength(200)]
        [Display(Name = "العنوان")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Display(Name = "المكلَّف")]
        public string? AssignedToId { get; set; }

        [ForeignKey("AssignedToId")]
        public virtual ApplicationUser? AssignedTo { get; set; }

        [Display(Name = "تاريخ الاستحقاق")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "الأولوية")]
        public Priority Priority { get; set; } = Priority.Medium;

        [Display(Name = "الحالة")]
        public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.NotStarted;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "مبلغ المكافأة")]
        public decimal BonusAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "مبلغ الغرامة")]
        public decimal PenaltyAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الإنجاز")]
        public decimal CompletionPercentage { get; set; }

        // المهام التي تعتمد عليها هذه المهمة
        public virtual ICollection<TaskDependency> Dependencies { get; set; } = new List<TaskDependency>();

        // المهام التي تعتمد على هذه المهمة
        public virtual ICollection<TaskDependency> DependentTasks { get; set; } = new List<TaskDependency>();
    }
}