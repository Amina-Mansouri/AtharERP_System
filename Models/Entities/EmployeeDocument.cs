using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class EmployeeDocument
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(255)]
        [Display(Name = "اسم الملف")]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "مسار الملف")]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "نوع الملف")]
        public string? FileType { get; set; }

        [Display(Name = "حجم الملف")]
        public long FileSize { get; set; }

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Display(Name = "تاريخ الرفع")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [ValidateNever]
        [Display(Name = "رفعه")]
        public string UploadedById { get; set; } = string.Empty;

        [ForeignKey("UploadedById")]
        [ValidateNever]
        public virtual ApplicationUser UploadedBy { get; set; } = null!;
    }
}