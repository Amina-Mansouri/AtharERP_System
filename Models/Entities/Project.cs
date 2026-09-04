using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "رمز المشروع")]
        [ValidateNever]
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
        [ValidateNever]
        public virtual Client Client { get; set; } = null!;

        [Display(Name = "المشروع الرئيسي")]
        public int? ParentProjectId { get; set; }

        [ForeignKey("ParentProjectId")]
        public virtual Project? ParentProject { get; set; }

        public virtual ICollection<Project> ChildProjects { get; set; } = new List<Project>();

        // رئيسي/فرعي — الاسم السابق لهذه الخاصية كان Type من نوع ProjectType (قبل إعادة التسمية)
        [Display(Name = "نطاق المشروع")]
        public ProjectScope Scope { get; set; } = ProjectScope.Main;

        // التصنيف القطاعي الجديد (بند P5) — منفصل تماماً عن Scope
        [Display(Name = "نوع المشروع")]
        public ProjectType? Type { get; set; }

        [Display(Name = "الحالة")]
        public ProjectStatus Status { get; set; } = ProjectStatus.New;

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ البدء المخطط")]
        public DateTime? PlannedStartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الانتهاء المخطط")]
        public DateTime? PlannedEndDate { get; set; }


        [DataType(DataType.Date)]
        [Display(Name = "تاريخ التسليم الفعلي")]
        public DateTime? ActualDeliveryDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الإنجاز")]
        public decimal CompletionPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الميزانية")]
        public decimal? Budget { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "التكلفة الفعلية")]
        public decimal ActualCost { get; set; }

        // محسوب من مجموع StageValue للمراحل — للقراءة فقط، لا يُقبل في [Bind] ولا يُعرض كحقل إدخال (بند L2)
        [NotMapped]
        [Display(Name = "إجمالي تكلفة المشروع")]
        public decimal TotalCost => Stages?.Sum(s => s.StageValue) ?? 0;

        [Display(Name = "الأولوية")]
        public Priority Priority { get; set; } = Priority.Normal;

        [Display(Name = "ترحيل تلقائي إلى إدارة المواقع عند بدء التنفيذ")]
        public bool AutoTransferToSite { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "أُنشئ بواسطة")]
        [ValidateNever]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        [ValidateNever]
        public virtual ApplicationUser CreatedBy { get; set; } = null!;

        public virtual ICollection<ProjectStage> Stages { get; set; } = new List<ProjectStage>();
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public virtual ICollection<ProjectTeamMember> TeamMembers { get; set; } = new List<ProjectTeamMember>();
        public virtual ICollection<ProjectAssignment> Assignments { get; set; } = new List<ProjectAssignment>();
        public virtual ICollection<ProjectDocument> Documents { get; set; } = new List<ProjectDocument>();
        public virtual ICollection<ProjectTimeline> Timelines { get; set; } = new List<ProjectTimeline>();
    }
}