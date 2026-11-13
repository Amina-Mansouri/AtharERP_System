using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // نموذج تسليم مشروع — مرتبط بمرحلة واحدة من مراحل المشروع، اعتماد أحادي (مهندسة تُنشئ، مدير يعتمد)
    public class ProjectDeliveryReport
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Required]
        public int ProjectStageId { get; set; }

        [ForeignKey("ProjectStageId")]
        [ValidateNever]
        public virtual ProjectStage Stage { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Display(Name = "رمز التقرير")]
        public string ReportCode { get; set; } = string.Empty;

        [Display(Name = "تاريخ التكليف")]
        public DateTime? AssignmentDate { get; set; }

        [Display(Name = "تاريخ التسليم الفعلي")]
        public DateTime? ActualDeliveryDate { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "الحالة")]
        public DeliveryReportStatus Status { get; set; } = DeliveryReportStatus.Pending;

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
        public virtual ICollection<ProjectDeliveryReportFile> DeliveredFiles { get; set; } = new List<ProjectDeliveryReportFile>();
    }
}