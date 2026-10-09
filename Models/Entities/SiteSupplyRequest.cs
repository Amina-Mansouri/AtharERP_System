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

        [Required]
        [Display(Name = "المشروع")]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "الحالة")]
        public SiteSupplyStatus Status { get; set; } = SiteSupplyStatus.Pending;

        [Display(Name = "تاريخ الطلب")]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        [Display(Name = "طلبه (موظف)")]
        public string? RequestedById { get; set; }

        [ForeignKey("RequestedById")]
        [ValidateNever]
        public virtual ApplicationUser? RequestedBy { get; set; }

        [Display(Name = "طلبه (مقاول)")]
        public int? RequestedByContractorId { get; set; }

        [ForeignKey("RequestedByContractorId")]
        [ValidateNever]
        public virtual Contractor? RequestedByContractor { get; set; }

        public virtual ICollection<SiteSupplyRequestItem> Items { get; set; } = new List<SiteSupplyRequestItem>();
    }
}