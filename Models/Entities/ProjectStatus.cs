using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ProjectStatus
    {
        [Display(Name = "جديد")]
        New = 1,

        [Display(Name = "قيد التنفيذ")]
        InProgress = 2,

        [Display(Name = "متوقف مؤقتاً")]
        OnHold = 3,

        [Display(Name = "مكتمل")]
        Completed = 4,

        [Display(Name = "ملغى")]
        Cancelled = 5,

        [Display(Name = "متأخر")]
        Delayed = 6
    }
}