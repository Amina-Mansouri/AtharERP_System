using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum SalaryAdvanceStatus
    {
        [Display(Name = "بانتظار الاعتماد")]
        Pending = 1,

        [Display(Name = "معتمدة")]
        Approved = 2,

        [Display(Name = "مرفوضة")]
        Rejected = 3
    }
}