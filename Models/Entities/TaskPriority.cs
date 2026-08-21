using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum TaskPriority
    {
        [Display(Name = "منخفضة")]
        Low = 1,

        [Display(Name = "متوسطة")]
        Medium = 2,

        [Display(Name = "عالية")]
        High = 3,

        [Display(Name = "حرجة")]
        Critical = 4
    }
}