using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // الطلب الفني — استفسار أو تعديل يرفعه الموقع للإدارة الهندسية، مختلف عن طلب التوريد.
    public class TechnicalRequest
    {
        public int Id { get; set; }

        [Required]
        public int SiteId { get; set; }

        [ForeignKey("SiteId")]
        [ValidateNever]
        public virtual Site Site { get; set; } = null!;

        [Display(Name = "المرحلة المرتبطة")]
        public int? StageId { get; set; }

        [ForeignKey("StageId")]
        public virtual ProjectStage? Stage { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "الرمز")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "نص الطلب مطلوب")]
        [Display(Name = "الطلب")]
        public string Request { get; set; } = string.Empty;

        [Required]
        [Display(Name = "الطالب")]
        [ValidateNever]
        public string RequestedById { get; set; } = string.Empty;

        [ForeignKey("RequestedById")]
        [ValidateNever]
        public virtual ApplicationUser RequestedBy { get; set; } = null!;

        [Display(Name = "التاريخ")]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Display(Name = "مُحوَّل إلى")]
        public int? RoutedToDepartmentId { get; set; }

        [ForeignKey("RoutedToDepartmentId")]
        public virtual Department? RoutedToDepartment { get; set; }

        [Display(Name = "أثر على التنفيذ")]
        public string? ImpactOnExecution { get; set; }

        [Display(Name = "الحالة")]
        public TechnicalRequestStatus Status { get; set; } = TechnicalRequestStatus.Open;
    }
}