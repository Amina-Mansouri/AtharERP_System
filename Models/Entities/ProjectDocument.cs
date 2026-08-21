using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ProjectDocument
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

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
        [Display(Name = "رفعه")]
        public string UploadedById { get; set; } = string.Empty;

        [ForeignKey("UploadedById")]
        public virtual ApplicationUser UploadedBy { get; set; } = null!;
    }
}