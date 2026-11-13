using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    // صف وحيد (Id=1) يحمل إعدادات النظام العامة القابلة للتعديل من المدير
    public class SystemSetting
    {
        public int Id { get; set; }

        [Display(Name = "عدد أيام الإجازة السنوية المسموح بها")]
        public int AnnualLeaveAllowanceDays { get; set; } = 20;
    }
}