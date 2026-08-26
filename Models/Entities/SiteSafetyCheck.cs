// Models/Entities/SiteSafetyCheck.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SiteSafetyCheck
    {
        public int Id { get; set; }

        [Required]
        public int SiteId { get; set; }

        [ForeignKey("SiteId")]
        [ValidateNever]
        public virtual Site Site { get; set; } = null!;

        [Required(ErrorMessage = "نوع الفحص مطلوب")]
        [StringLength(255)]
        [Display(Name = "نوع الفحص")]
        public string CheckType { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Display(Name = "النتيجة")]
        public SafetyResult Result { get; set; } = SafetyResult.Safe;

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "تاريخ الفحص")]
        public DateTime CheckDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "فحصه")]
        public string CheckedById { get; set; } = string.Empty;

        [ForeignKey("CheckedById")]
        [ValidateNever]
        public virtual ApplicationUser CheckedBy { get; set; } = null!;

        [Display(Name = "معتمد")]
        public bool IsApproved { get; set; }
    }
}