using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class TaskAssignee
    {
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        [ValidateNever]
        public virtual ProjectTask Task { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser User { get; set; } = null!;

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة المساهمة")]
        public decimal ContributionPercentage { get; set; } = 100;

        [Display(Name = "تاريخ التكليف")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}