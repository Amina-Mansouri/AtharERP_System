// Models/Entities/SiteSupplyRequest.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SiteSupplyRequest
    {
        public int Id { get; set; }

        [Required]
        public int SiteId { get; set; }

        [ForeignKey("SiteId")]
        [ValidateNever]
        public virtual Site Site { get; set; } = null!;

        // تُملأ تلقائياً من مشروع الموقع (site.ProjectId) في الكنترولر، وليست ضمن أي [Bind]
        [Required]
        [Display(Name = "المشروع")]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Required(ErrorMessage = "اسم المادة مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم المادة")]
        public string MaterialName { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "الأبعاد")]
        public string? Dimensions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الكمية")]
        public decimal Quantity { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "الوحدة")]
        public string Unit { get; set; } = string.Empty;

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "الحالة")]
        public SiteSupplyStatus Status { get; set; } = SiteSupplyStatus.Pending;

        [Display(Name = "تاريخ الطلب")]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "طلبه")]
        public string RequestedById { get; set; } = string.Empty;

        [ForeignKey("RequestedById")]
        [ValidateNever]
        public virtual ApplicationUser RequestedBy { get; set; } = null!;
    }
}