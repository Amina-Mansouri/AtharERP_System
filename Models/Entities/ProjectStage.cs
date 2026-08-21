using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class ProjectStage
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Required(ErrorMessage = "اسم المرحلة مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم المرحلة")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "الترتيب")]
        public int Sequence { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "الوزن (% من المشروع)")]
        public decimal Weight { get; set; }

        [Display(Name = "الحالة")]
        public StageStatus Status { get; set; } = StageStatus.New;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "التكلفة المخصصة")]
        public decimal? Cost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "التكلفة الفعلية")]
        public decimal ActualCost { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الإنجاز")]
        public decimal CompletionPercentage { get; set; }

        [Display(Name = "المهندس المسؤول")]
        public string? AssignedEngineerId { get; set; }

        [ForeignKey("AssignedEngineerId")]
        public virtual ApplicationUser? AssignedEngineer { get; set; }

        [Display(Name = "القسم")]
        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

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

        [Display(Name = "توثيق العمل الدوري")]
        public string? WorkDocumentation { get; set; }

        public virtual ICollection<ProjectStep> Steps { get; set; } = new List<ProjectStep>();
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    }
}