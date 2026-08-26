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

        [Required(ErrorMessage = "اسم المقاول مطلوب")]
        [StringLength(255)]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "اسم الشركة")]
        public string? CompanyName { get; set; }

        [StringLength(50)]
        [Display(Name = "الهاتف")]
        public string? Phone { get; set; }

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