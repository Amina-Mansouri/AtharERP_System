// Models/Entities/SiteDailyReportPhoto.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SiteDailyReportPhoto
    {
        public int Id { get; set; }

        [Required]
        public int DailyReportId { get; set; }

        [ForeignKey("DailyReportId")]
        [ValidateNever]
        public virtual SiteDailyReport DailyReport { get; set; } = null!;

        [Required]
        [StringLength(500)]
        [Display(Name = "مسار الملف")]
        public string FilePath { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Display(Name = "تاريخ الرفع")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}