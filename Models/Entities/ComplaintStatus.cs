using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ComplaintStatus
    {
        [Display(Name = "بانتظار الرد")]
        Pending = 1,

        [Display(Name = "تم الرد")]
        Resolved = 2
    }
}