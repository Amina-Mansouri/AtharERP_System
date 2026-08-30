using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    // تفضيل تفعيل/تعطيل حدث إشعار بعينه لكل مستخدم (البند 3 في BACKEND.md)
    public class NotificationSetting
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Display(Name = "نوع الحدث")]
        public NotificationEventType EventType { get; set; }

        [Display(Name = "مفعّل")]
        public bool IsEnabled { get; set; } = true;
    }
}