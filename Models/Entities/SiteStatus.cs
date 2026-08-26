// Models/Entities/SiteStatus.cs
using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum SiteStatus
    {
        [Display(Name = "نشط")] Active = 1,
        [Display(Name = "متوقف")] OnHold = 2,
        [Display(Name = "مكتمل")] Completed = 3
    }
}