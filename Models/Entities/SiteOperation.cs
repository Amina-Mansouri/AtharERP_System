// Models/Entities/SiteOperation.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SiteOperation
    {
        public int Id { get; set; }

        [Required]
        public int SiteId { get; set; }

        [ForeignKey("SiteId")]
        [ValidateNever]
        public virtual Site Site { get; set; } = null!;

        [Required(ErrorMessage = "اسم المرحلة مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم المرحلة")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Display(Name = "الترتيب")]
        public int Sequence { get; set; }

        [Display(Name = "الحالة")]
        public OperationStatus Status { get; set; } = OperationStatus.NotStarted;

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

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
    }
}