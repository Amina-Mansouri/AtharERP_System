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

        [Required(ErrorMessage = "نوع التكلفة مطلوب")]
        [StringLength(100)]
        [Display(Name = "نوع التكلفة")]
        public string CostType { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        // محسوبة تلقائياً = مجموع القيم التقديرية لكل مهام هذا التكليف — لا تُدخل يدوياً أبداً
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "القيمة الإجمالية")]
        public decimal FinalAmount { get; set; }

        [Display(Name = "الحالة")]
        public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;

        [Display(Name = "عاجل")]
        public bool IsUrgent { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الاستلام")]
        public DateTime? ReceivedDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "التاريخ المتفق عليه")]
        public DateTime? AgreedDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "التاريخ الفعلي")]
        public DateTime? ActualDate { get; set; }

        [Display(Name = "مرحّل إلى المالية")]
        public bool IsTransferredToFinance { get; set; }

        [Display(Name = "تاريخ الترحيل")]
        public DateTime? TransferredToFinanceAt { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ProjectAssignmentSubtask> Subtasks { get; set; } = new List<ProjectAssignmentSubtask>();
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();

        public virtual ICollection<AssignmentEngineer> Engineers { get; set; } = new List<AssignmentEngineer>();
    }
}