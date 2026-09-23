using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // سجل مراجعة واحد لمقترح تصميمي: نتيجة الاعتماد/الرفض بتوقيعين إلكترونيين وملف PDF مولَّد
    public class ProposalReview
    {
        public int Id { get; set; }

        [Required]
        public int DesignProposalId { get; set; }

        [ForeignKey("DesignProposalId")]
        [ValidateNever]
        public virtual DesignProposal DesignProposal { get; set; } = null!;

        [Display(Name = "الحالة")]
        public ProposalStatus Status { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "اسم المراجع")]
        public string ReviewerName { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "وظيفة المراجع")]
        public string? ReviewerPosition { get; set; }

        [StringLength(500)]
        [Display(Name = "توقيع المراجع")]
        public string? ReviewerSignaturePath { get; set; }

        [StringLength(255)]
        [Display(Name = "اسم المشرف المباشر")]
        public string? SupervisorName { get; set; }

        [StringLength(255)]
        [Display(Name = "وظيفة المشرف المباشر")]
        public string? SupervisorPosition { get; set; }

        [StringLength(500)]
        [Display(Name = "توقيع المشرف المباشر")]
        public string? SupervisorSignaturePath { get; set; }

        [Display(Name = "الملاحظات")]
        public string? Notes { get; set; }

        // يدوي مؤقتاً حتى تُحدَّد طبيعته (تصنيف ثابت أم جدول مستقل لاحقاً)
        [StringLength(255)]
        [Display(Name = "التخصص")]
        public string? Discipline { get; set; }

        [Display(Name = "تاريخ المراجعة")]
        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        [Display(Name = "مسار ملف PDF")]
        public string? PdfFilePath { get; set; }

        [Required]
        [Display(Name = "راجعه")]
        [ValidateNever]
        public string ReviewedById { get; set; } = string.Empty;

        [ForeignKey("ReviewedById")]
        [ValidateNever]
        public virtual ApplicationUser ReviewedBy { get; set; } = null!;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}