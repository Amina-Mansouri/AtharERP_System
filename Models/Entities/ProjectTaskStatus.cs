using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ProjectTaskStatus
    {
        [Display(Name = "لم تبدأ")]
        NotStarted = 0,

        [Display(Name = "قيد التنفيذ")]
        InProgress = 1,

        [Display(Name = "مكتملة")]
        Completed = 2,

        [Display(Name = "معلّقة")]
        Blocked = 3
    }
}