using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class ProjectExpense
    {
        public int Id { get; set; }

        public int? ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project? Project { get; set; }

        [Display(Name = "الموقع")]
        public int? SiteId { get; set; }

        [ForeignKey("SiteId")]
        [ValidateNever]
        public virtual Site? Site { get; set; }

        [Required]
        public int ExpenseCategoryId { get; set; }

        [ForeignKey("ExpenseCategoryId")]
        [ValidateNever]
        public virtual ExpenseCategory ExpenseCategory { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "التاريخ")]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Display(Name = "الوصف")]
        public string? Description { get; set; }
        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? EngineerId { get; set; }
        [ForeignKey("EngineerId")]
        [ValidateNever]
        public virtual ApplicationUser? Engineer { get; set; }

        public string? CreatedById { get; set; }
        [ForeignKey("CreatedById")]
        [ValidateNever]
        public virtual ApplicationUser? CreatedBy { get; set; }

        public string? PdfFilePath { get; set; }
    }
}