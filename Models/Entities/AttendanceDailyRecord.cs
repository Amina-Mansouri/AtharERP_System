using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // ملخّص حضور يوم واحد لموظف واحد — محسوب من AttendancePunch عند كل مزامنة
    public class AttendanceDailyRecord
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser User { get; set; } = null!;

        [Display(Name = "التاريخ")]
        public DateTime Date { get; set; }

        [Display(Name = "وقت الحضور")]
        public DateTime? CheckIn { get; set; }

        [Display(Name = "وقت الانصراف")]
        public DateTime? CheckOut { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "إجمالي ساعات العمل")]
        public decimal? TotalHours { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "دقائق التأخير")]
        public decimal LateMinutes { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "دقائق الانصراف المبكر")]
        public decimal EarlyLeaveMinutes { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "ساعات إضافية")]
        public decimal OvertimeHours { get; set; }

        // null = لم يُبتّ فيها بعد، true = معتمدة كساعات، false = مرفوضة
        [Display(Name = "اعتماد الإضافي")]
        public bool? OvertimeApproved { get; set; }

        public string? OvertimeDecidedById { get; set; }

        [ForeignKey("OvertimeDecidedById")]
        [ValidateNever]
        public virtual ApplicationUser? OvertimeDecidedBy { get; set; }

        [Display(Name = "تاريخ اعتماد الإضافي")]
        public DateTime? OvertimeDecidedAt { get; set; }
    }
}