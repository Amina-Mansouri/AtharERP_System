using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // إصدارات ضبط الوثائق — إصدار واحد فقط معتمد لكل وثيقة، والباقي يُؤرشف بلا حذف.
    public class DocumentRevision
    {
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        [ValidateNever]
        public virtual ProjectDocument Document { get; set; } = null!;

        [StringLength(100)]
        [Display(Name = "التصنيف")]
        public string? Category { get; set; }

        [Display(Name = "الإصدار")]
        public int Revision { get; set; } = 1;

        [Required]
        [Display(Name = "أعدّها")]
        [ValidateNever]
        public string PreparedById { get; set; } = string.Empty;

        [ForeignKey("PreparedById")]
        [ValidateNever]
        public virtual ApplicationUser PreparedBy { get; set; } = null!;

        [DataType(DataType.Date)]
        [Display(Name = "التاريخ")]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Display(Name = "الحالة")]
        public RevisionStatus Status { get; set; } = RevisionStatus.Pending;
    }
}