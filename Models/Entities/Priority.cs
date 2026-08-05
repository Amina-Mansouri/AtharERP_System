using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum Priority
    {
        [Display(Name = "منخفضة")]
        Low = 0,

        [Display(Name = "متوسطة")]
        Medium = 1,

        [Display(Name = "عالية")]
        High = 2,

        [Display(Name = "حرجة")]
        Critical = 3
    }
}