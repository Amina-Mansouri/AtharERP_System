using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "رمز المشروع")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم المشروع مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم المشروع")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "العميل")]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; } = null!;

        [Display(Name = "المشروع الرئيسي")]
        public int? ParentProjectId { get; set; }

        [ForeignKey("ParentProjectId")]
        public virtual Project? ParentProject { get; set; }

        public virtual ICollection<Project> ChildProjects { get; set; } = new List<Project>();

        [Display(Name = "نوع المشروع")]
        public ProjectType Type { get; set; } = ProjectType.Main;

        [Display(Name = "الحالة")]
        public ProjectStatus Status { get; set; } = ProjectStatus.New;

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ البدء المخطط")]
        public DateTime? PlannedStartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الانتهاء المخطط")]
        public DateTime? PlannedEndDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ البدء الفعلي")]
        public DateTime? ActualStartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الانتهاء الفعلي")]
        public DateTime? ActualEndDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الإنجاز")]
        public decimal CompletionPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الميزانية")]
        public decimal? Budget { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "التكلفة الفعلية")]
        public decimal ActualCost { get; set; }

        [Display(Name = "الأولوية")]
        public Priority Priority { get; set; } = Priority.Normal;

        [Display(Name = "عاجل")]
        public bool IsUrgent { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "أُنشئ بواسطة")]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        public virtual ApplicationUser CreatedBy { get; set; } = null!;

        public virtual ICollection<ProjectStage> Stages { get; set; } = new List<ProjectStage>();
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public virtual ICollection<ProjectTeamMember> TeamMembers { get; set; } = new List<ProjectTeamMember>();
        public virtual ICollection<ProjectCost> Costs { get; set; } = new List<ProjectCost>();
        public virtual ICollection<ProjectDocument> Documents { get; set; } = new List<ProjectDocument>();
        public virtual ICollection<ProjectTimeline> Timelines { get; set; } = new List<ProjectTimeline>();
    }
}