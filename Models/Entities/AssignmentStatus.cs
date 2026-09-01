using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum AssignmentStatus
    {
        [Display(Name = "معلق")]
        Pending = 1,

        [Display(Name = "قيد التنفيذ")]
        InProgress = 2,

        [Display(Name = "مكتمل")]
        Completed = 3,

        [Display(Name = "ملغى")]
        Cancelled = 4
    }
}