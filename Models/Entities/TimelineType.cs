using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum TimelineType
    {
        [Display(Name = "مرحلة")]
        Stage = 1,

        [Display(Name = "مهمة")]
        Task = 2,

        [Display(Name = "حدث رئيسي")]
        Milestone = 3
    }
}