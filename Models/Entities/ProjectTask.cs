using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ProjectTask
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [Display(Name = "المرحلة")]
        public int? StageId { get; set; }

        [ForeignKey("StageId")]
        public virtual ProjectStage? Stage { get; set; }

        [Required(ErrorMessage = "عنوان المهمة مطلوب")]
        [StringLength(255)]
        [Display(Name = "العنوان")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الاستحقاق")]
        public DateTime? DueDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ البدء المخطط")]
        public DateTime? PlannedStartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الانتهاء المخطط")]
        public DateTime? PlannedEndDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ التسليم الفعلي")]
        public DateTime? ActualDeliveryDate { get; set; }

        [Display(Name = "أيام التأخير")]
        public int DelayDays { get; set; }

        [Display(Name = "أيام التسليم المبكر")]
        public int EarlyDeliveryDays { get; set; }

        [Display(Name = "الأولوية")]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [Display(Name = "الحالة")]
        public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.NotStarted;

        [Display(Name = "عاجلة")]
        public bool IsUrgent { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الإنجاز")]
        public decimal CompletionPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "مبلغ المكافأة")]
        public decimal BonusAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "مبلغ الغرامة")]
        public decimal PenaltyAmount { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "أُنشئت بواسطة")]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        public virtual ApplicationUser CreatedBy { get; set; } = null!;

        public virtual ICollection<TaskAssignee> Assignees { get; set; } = new List<TaskAssignee>();
        public virtual ICollection<TaskTodo> Todos { get; set; } = new List<TaskTodo>();
        public virtual ICollection<TaskDependency> Dependencies { get; set; } = new List<TaskDependency>();
        public virtual ICollection<TaskDependency> DependentTasks { get; set; } = new List<TaskDependency>();
    }
}