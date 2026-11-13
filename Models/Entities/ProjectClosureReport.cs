using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // تقرير إغلاق وتسليم المشاريع — اعتماد أحادي: المهندسة تُنشئ، المدير يعتمد ويُدخل حالة المشروع النهائية عند اعتماده
    public class ProjectClosureReport
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Display(Name = "رمز التقرير")]
        public string ReportCode { get; set; } = string.Empty;

        [Display(Name = "الاصدار")]
        public string? Version { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الإغلاق")]
        public DateTime? ClosureDate { get; set; }

        [Display(Name = "تم استكمال كامل نطاق العمل")]
        public bool ScopeFullyCompleted { get; set; }

        [Display(Name = "تم استكمال جزئي مع ملاحظات")]
        public bool ScopePartiallyWithNotes { get; set; }

        [Display(Name = "توجد أعمال مستثناة من نطاق العقد")]
        public bool ScopeExcludedWorkExists { get; set; }

        [Display(Name = "ملاحظات نطاق العمل")]
        public string? ScopeNotes { get; set; }

        [Display(Name = "حالة المشروع النهائية")]
        public ClosureFinalStatus? FinalStatus { get; set; }

        [Display(Name = "ملاحظات الحالة النهائية")]
        public string? FinalStatusNotes { get; set; }

        [Display(Name = "الحالة")]
        public ClosureReportStatus Status { get; set; } = ClosureReportStatus.Pending;

        [Required]
        [ValidateNever]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        [ValidateNever]
        public virtual ApplicationUser CreatedBy { get; set; } = null!;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public string? DecidedById { get; set; }

        [ForeignKey("DecidedById")]
        [ValidateNever]
        public virtual ApplicationUser? DecidedBy { get; set; }

        [Display(Name = "تاريخ الاعتماد")]
        public DateTime? DecidedAt { get; set; }

        [Display(Name = "مسار ملف PDF")]
        public string? PdfFilePath { get; set; }

        [ValidateNever]
        public virtual ICollection<ProjectClosureReportStage> Stages { get; set; } = new List<ProjectClosureReportStage>();
    }
}