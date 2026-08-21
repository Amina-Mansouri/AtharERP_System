using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum StageStatus
    {
        [Display(Name = "جديدة")]
        New = 1,

        [Display(Name = "قيد التنفيذ")]
        InProgress = 2,

        [Display(Name = "مراجعة العميل")]
        ClientReview = 3,

        [Display(Name = "مكتملة")]
        Completed = 4,

        [Display(Name = "متأخرة")]
        Delayed = 5
    }
}