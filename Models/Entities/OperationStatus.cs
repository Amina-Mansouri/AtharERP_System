// Models/Entities/OperationStatus.cs
using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum OperationStatus
    {
        [Display(Name = "لم تبدأ")] NotStarted = 1,
        [Display(Name = "قيد التنفيذ")] InProgress = 2,
        [Display(Name = "مكتملة")] Completed = 3,
        [Display(Name = "متأخرة")] Delayed = 4
    }
}