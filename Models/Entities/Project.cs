using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "رمز المشروع")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم المشروع مطلوب")]
        [StringLength(200)]
        [Display(Name = "اسم المشروع")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [StringLength(250)]
        [Display(Name = "الموقع")]
        public string? Location { get; set; }

        [Display(Name = "خط العرض")]
        public double? Latitude { get; set; }

        [Display(Name = "خط الطول")]
        public double? Longitude { get; set; }

        [Required(ErrorMessage = "تاريخ البدء مطلوب")]
        [DataType(DataType.Date)]
        [Display(Name = "تاريخ البدء")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الانتهاء المتوقع")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "الحالة")]
        public ProjectStatus Status { get; set; } = ProjectStatus.New;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الميزانية")]
        public decimal Budget { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ========== التسلسل الهرمي: مشروع رئيسي / فرعي ==========
        [Display(Name = "المشروع الرئيسي")]
        public int? ParentProjectId { get; set; }

        [ForeignKey("ParentProjectId")]
        public virtual Project? ParentProject { get; set; }

        public virtual ICollection<Project> ChildProjects { get; set; } = new List<Project>();

        // ========== مدير المشروع ==========
        [Display(Name = "مدير المشروع")]
        public string? ProjectManagerId { get; set; }

        [ForeignKey("ProjectManagerId")]
        public virtual ApplicationUser? ProjectManager { get; set; }

        // ========== المهندسون المكلفون ==========
        public virtual ICollection<ProjectEngineer> ProjectEngineers { get; set; } = new List<ProjectEngineer>();
    }
}