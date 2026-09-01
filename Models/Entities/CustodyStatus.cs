using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum CustodyStatus
    {
        [Display(Name = "بحوزة موظف")]
        WithEmployee = 1,

        [Display(Name = "مُعادة")]
        Returned = 2,

        [Display(Name = "تحتاج إعادة")]
        NeedsReturn = 3
    }
}