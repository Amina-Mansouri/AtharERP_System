// Models/Entities/SiteSupplyStatus.cs
using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum SiteSupplyStatus
    {
        [Display(Name = "معلق")] Pending = 1,
        [Display(Name = "تمت الموافقة")] Approved = 2,
        [Display(Name = "تم التسليم")] Delivered = 3,
        [Display(Name = "مرفوض")] Rejected = 4
    }
}