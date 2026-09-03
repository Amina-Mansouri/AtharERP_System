using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class ContractHistory
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser User { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الراتب التعاقدي")]
        public decimal ContractSalary { get; set; }

        [Display(Name = "تاريخ بداية العقد")]
        public DateTime? ContractStartDate { get; set; }

        [Display(Name = "تاريخ نهاية العقد")]
        public DateTime? ContractEndDate { get; set; }

        [Display(Name = "صورة العقد")]
        public string? ContractImagePath { get; set; }

        [Display(Name = "تاريخ الأرشفة")]
        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
    }
}