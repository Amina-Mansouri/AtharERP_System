using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ProjectStatus
    {
        [Display(Name = "جديد")]
        New = 0,

        [Display(Name = "قيد التنفيذ")]
        InProgress = 1,

        [Display(Name = "مراجعة العميل")]
        ClientReview = 2,

        [Display(Name = "مكتمل")]
        Completed = 3
    }
}