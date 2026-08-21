using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Display(Name = "العملية")]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "نوع الكيان")]
        public string EntityName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "معرّف الكيان")]
        public string EntityId { get; set; } = string.Empty;

        [Display(Name = "تفاصيل")]
        public string? Details { get; set; }

        [Display(Name = "الوقت")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}