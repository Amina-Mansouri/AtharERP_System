using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // مهندسات التكليف — بدون تمييز رئيسي/مساعد، عدد غير محدود لكل تكليف
    public class AssignmentEngineer
    {
        public int Id { get; set; }

        [Required]
        public int ProjectAssignmentId { get; set; }

        [ForeignKey("ProjectAssignmentId")]
        [ValidateNever]
        public virtual ProjectAssignment ProjectAssignment { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}