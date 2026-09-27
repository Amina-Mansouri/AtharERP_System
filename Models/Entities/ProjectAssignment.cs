using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // التكليف — الاسم السابق ProjectCost كان مضلِّلاً (يمثّل تكليفاً لا تكلفة مالية حقيقية)
    // أُعيدت التسمية حسب 06-CONFLICTS.md · C7؛ Area/PricePerMeter تخصّ تسعير هذا التكليف
    // نفسه، منفصلة عن ProjectStage.Area/PricePerMeter/StageValue (قيمة المرحلة — C1).
    public class ProjectAssignment
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Display(Name = "المرحلة")]
        public int? StageId { get; set; }

        [ForeignKey("StageId")]
        public virtual ProjectStage? Stage { get; set; }

        // الاسم السابق كان CostType — يتعارض مع FinancialRecord.CostType (نوع تكلفة مالية حقيقي)، بينما هذا الحقل يصف نوع/تخصص التكليف نفسه لا تكلفته
        [Required(ErrorMessage = "نوع التكليف مطلوب")]
        [StringLength(100)]
        [Display(Name = "نوع التكليف")]
        public string AssignmentType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المساحة (م²)")]
        public decimal? Area { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "سعر متر التكليف")]
        public decimal? PricePerMeter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "سعر متر البيع")]
        public decimal? SalePricePerMeter { get; set; }

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Display(Name = "الحالة")]
        public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;

        [Display(Name = "عاجل")]
        public bool IsUrgent { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ البداية")]
        public DateTime? PlannedStartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ النهاية")]
        public DateTime? PlannedEndDate { get; set; }

        [Display(Name = "مرحّل إلى المالية")]
        public bool IsTransferredToFinance { get; set; }

        [Display(Name = "تاريخ الترحيل")]
        public DateTime? TransferredToFinanceAt { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        [Display(Name = "قيمة التكليف")]
        public decimal AssignmentValue => (Area ?? 0) * (PricePerMeter ?? 0);

        [NotMapped]
        [Display(Name = "قيمة البيع")]
        public decimal AssignmentSaleValue => (Area ?? 0) * (SalePricePerMeter ?? 0);
        public virtual ICollection<ProjectAssignmentSubtask> Subtasks { get; set; } = new List<ProjectAssignmentSubtask>();
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();

        public virtual ICollection<AssignmentEngineer> Engineers { get; set; } = new List<AssignmentEngineer>();
    }
}