// Models/Entities/SiteDailyReport.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SiteDailyReport
    {
        public int Id { get; set; }

        [Required]
        public int SiteId { get; set; }

        [ForeignKey("SiteId")]
        [ValidateNever]
        public virtual Site Site { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "تاريخ التقرير")]
        public DateTime ReportDate { get; set; }

        [StringLength(100)]
        [Display(Name = "حالة الطقس")]
        public string? Weather { get; set; }

        [Display(Name = "عدد العمال")]
        public int WorkersCount { get; set; }

        [Display(Name = "الأعمال المنجزة")]
        public string? WorkCompleted { get; set; }

        [Display(Name = "المشاكل والعوائق")]
        public string? Issues { get; set; }

        [Display(Name = "المواد المستخدمة")]
        public string? MaterialsUsed { get; set; }

        [Display(Name = "المعدات المستخدمة")]
        public string? EquipmentUsed { get; set; }

        [Display(Name = "الزيارات")]
        public string? Visits { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Required]
        [ValidateNever]
        [Display(Name = "أُنشئ بواسطة")]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        [ValidateNever]
        public virtual ApplicationUser CreatedBy { get; set; } = null!;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<SiteDailyReportPhoto> Photos { get; set; } = new List<SiteDailyReportPhoto>();
    }
}