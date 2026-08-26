// Models/Entities/SiteDocument.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SiteDocument
    {
        public int Id { get; set; }

        [Required]
        public int SiteId { get; set; }

        [ForeignKey("SiteId")]
        [ValidateNever]
        public virtual Site Site { get; set; } = null!;

        [Display(Name = "نوع المستند")]
        public SiteDocumentType DocumentType { get; set; } = SiteDocumentType.Other;

        [Required]
        [StringLength(255)]
        [Display(Name = "اسم الملف")]
        public string FileName { get; set; } = string.Empty;

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