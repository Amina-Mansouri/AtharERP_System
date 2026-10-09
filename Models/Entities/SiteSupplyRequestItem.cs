using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SiteSupplyRequestItem
    {
        public int Id { get; set; }

        [Required]
        public int SiteSupplyRequestId { get; set; }

        [ForeignKey("SiteSupplyRequestId")]
        [ValidateNever]
        public virtual SiteSupplyRequest SiteSupplyRequest { get; set; } = null!;

        [Required(ErrorMessage = "اسم المادة مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم المادة")]
        public string MaterialName { get; set; } = string.Empty;

        [Display(Name = "صورة المادة")]
        public string? PhotoPath { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الكمية")]
        public decimal Quantity { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "الوحدة")]
        public string Unit { get; set; } = string.Empty;

        [Display(Name = "المورّد (اختياري)")]
        public int? VendorId { get; set; }

        [ForeignKey("VendorId")]
        [ValidateNever]
        public virtual Vendor? Vendor { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
    }
}