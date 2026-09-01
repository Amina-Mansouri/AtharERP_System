using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // المقترح التصميمي (وثيقة ١٣-٨ · Z4) — يمكن تسليم أكثر من نسخة حتى الاعتماد،
    // والمرفوض يبقى محفوظاً بسبب رفضه ولا يُحذف.
    public class DesignProposal
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Display(Name = "الرمز")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم المقترح مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم المقترح")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "النسخة")]
        public int Revision { get; set; } = 1;

        [Required]
        [Display(Name = "أعدّه")]
        [ValidateNever]
        public string PreparedById { get; set; } = string.Empty;

        [ForeignKey("PreparedById")]
        [ValidateNever]
        public virtual ApplicationUser PreparedBy { get; set; } = null!;

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ التسليم")]
        public DateTime? SubmittedDate { get; set; }

        [Display(Name = "ردّ العميل")]
        public string? ClientReply { get; set; }

        [Display(Name = "الحالة")]
        public ProposalStatus Status { get; set; } = ProposalStatus.Submitted;

        [Display(Name = "سبب الرفض")]
        public string? RejectReason { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}