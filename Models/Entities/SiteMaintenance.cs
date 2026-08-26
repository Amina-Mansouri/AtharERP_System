// Models/Entities/SiteMaintenance.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SiteMaintenance
    {
        public int Id { get; set; }

        [Required]
        public int SiteId { get; set; }

        [ForeignKey("SiteId")]
        [ValidateNever]
        public virtual Site Site { get; set; } = null!;

        [Required(ErrorMessage = "نوع الصيانة مطلوب")]
        [StringLength(255)]
        [Display(Name = "نوع الصيانة")]
        public string MaintenanceType { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Display(Name = "الحالة")]
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;

        [Display(Name = "تاريخ الطلب")]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "تاريخ الإنجاز")]
        public DateTime? CompletionDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "التكلفة")]
        public decimal? Cost { get; set; }

        [Display(Name = "المسؤول")]
        public string? ResponsibleId { get; set; }

        [ForeignKey("ResponsibleId")]
        [ValidateNever]
        public virtual ApplicationUser? Responsible { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
    }
}