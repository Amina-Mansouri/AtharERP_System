using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SiteRequirement
    {
        public int Id { get; set; }

        [Required]
        public int SiteId { get; set; }

        [ForeignKey("SiteId")]
        [ValidateNever]
        public virtual Site Site { get; set; } = null!;

        [Required(ErrorMessage = "نص المتطلب مطلوب")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "المرفق")]
        public string? AttachmentPath { get; set; }

        [Display(Name = "اسم ملف المرفق")]
        public string? AttachmentFileName { get; set; }

        [Display(Name = "من المقاول")]
        public bool IsFromContractor { get; set; }

        [ValidateNever]
        public string? SentById { get; set; }

        [ForeignKey("SentById")]
        [ValidateNever]
        public virtual ApplicationUser? SentBy { get; set; }

        public int? SentByContractorId { get; set; }

        [ForeignKey("SentByContractorId")]
        [ValidateNever]
        public virtual Contractor? SentByContractor { get; set; }

        [Display(Name = "مقروء")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "تاريخ الإرسال")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}