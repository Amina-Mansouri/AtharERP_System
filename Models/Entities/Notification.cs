using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(500)]
        [Display(Name = "الرسالة")]
        public string Message { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "رابط")]
        public string? Link { get; set; }

        [Display(Name = "مقروء")]
        public bool IsRead { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "نوع الحدث")]
        public NotificationEventType EventType { get; set; }

        [Required]
        [StringLength(10)]
        [Display(Name = "الوحدة المصدر")]
        public string SourceModule { get; set; } = "02";

        [Display(Name = "تحتاج إجراءً")]
        public bool RequiresAction { get; set; }

        [StringLength(50)]
        [Display(Name = "نوع الكيان")]
        public string? EntityType { get; set; }

        [Display(Name = "معرّف الكيان")]
        public int? EntityId { get; set; }
    }
}