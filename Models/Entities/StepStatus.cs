using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum StepStatus
    {
        [Display(Name = "لم تبدأ")]
        NotStarted = 1,

        [Display(Name = "قيد التنفيذ")]
        InProgress = 2,

        [Display(Name = "مكتملة")]
        Completed = 3
    }
}