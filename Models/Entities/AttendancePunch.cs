using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // بصمة خام واحدة (دخول أو خروج) مستوردة من جهاز BioTime عبر الوكيل الجسر
    public class AttendancePunch
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser User { get; set; } = null!;

        [Display(Name = "وقت البصمة")]
        public DateTime PunchTime { get; set; }

        // رقم العملية (id) من BioTime نفسه — يمنع استيراد نفس البصمة مرتين عند إعادة المزامنة
        [Required]
        [StringLength(100)]
        [Display(Name = "رقم العملية المصدري")]
        public string SourceTransactionId { get; set; } = string.Empty;

        [Display(Name = "تاريخ الاستيراد")]
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }
}