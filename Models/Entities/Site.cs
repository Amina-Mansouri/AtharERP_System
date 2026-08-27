// Models/Entities/Site.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class Site
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الموقع مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم الموقع")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "المشروع")]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Display(Name = "العنوان")]
        public string? Address { get; set; }

        [Display(Name = "خط العرض")]
        public double? Latitude { get; set; }

        [Display(Name = "خط الطول")]
        public double? Longitude { get; set; }

       
        [Display(Name = "الحالة")]
        public SiteStatus Status { get; set; } = SiteStatus.Active;

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ البدء")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الانتهاء المتوقع")]
        public DateTime? ExpectedEndDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الانتهاء الفعلي")]
        public DateTime? ActualEndDate { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<SiteOperation> Operations { get; set; } = new List<SiteOperation>();
        public virtual ICollection<SiteDailyReport> DailyReports { get; set; } = new List<SiteDailyReport>();
        public virtual ICollection<SiteQualityCheck> QualityChecks { get; set; } = new List<SiteQualityCheck>();
        public virtual ICollection<SiteSafetyCheck> SafetyChecks { get; set; } = new List<SiteSafetyCheck>();
        public virtual ICollection<SiteContractor> Contractors { get; set; } = new List<SiteContractor>();
        public virtual ICollection<SiteMaintenance> MaintenanceRequests { get; set; } = new List<SiteMaintenance>();
        public virtual ICollection<SiteDocument> Documents { get; set; } = new List<SiteDocument>();
        public virtual ICollection<SiteSupplyRequest> SupplyRequests { get; set; } = new List<SiteSupplyRequest>();
    }
}