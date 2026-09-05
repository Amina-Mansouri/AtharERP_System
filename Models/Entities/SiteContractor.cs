// Models/Entities/SiteContractor.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SiteContractor
    {
        public int Id { get; set; }

        [Required]
        public int SiteId { get; set; }

        [ForeignKey("SiteId")]
        [ValidateNever]
        public virtual Site Site { get; set; } = null!;

        [Required]
        public int ContractorId { get; set; }

        [ForeignKey("ContractorId")]
        [ValidateNever]
        public virtual Contractor Contractor { get; set; } = null!;

        [StringLength(255)]
        [Display(Name = "التخصص")]
        public string? Specialty { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ البدء")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الانتهاء")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "الحالة")]
        public ContractorStatus Status { get; set; } = ContractorStatus.Active;

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
    }
}