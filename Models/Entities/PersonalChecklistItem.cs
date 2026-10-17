using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class PersonalChecklistItem
    {
        public int Id { get; set; }

        [Required]
        public int ProjectAssignmentId { get; set; }

        [ForeignKey("ProjectAssignmentId")]
        [ValidateNever]
        public virtual ProjectAssignment ProjectAssignment { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "نص العنصر مطلوب")]
        [StringLength(300)]
        public string Text { get; set; } = string.Empty;

        public bool IsChecked { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}