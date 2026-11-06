using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum LeaveType
    {
        [Display(Name = "سنوية")]
        Annual = 1,

        [Display(Name = "مرضية")]
        Sick = 2,

        [Display(Name = "استثنائية بدون راتب")]
        Unpaid = 3,

        [Display(Name = "أخرى")]
        Other = 4
    }
}