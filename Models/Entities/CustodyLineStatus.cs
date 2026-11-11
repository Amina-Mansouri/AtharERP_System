using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum CustodyLineStatus
    {
        [Display(Name = "بحوزة الموظف")]
        WithEmployee = 1,

        [Display(Name = "مُعادة جزئياً")]
        PartiallyReturned = 2,

        [Display(Name = "مُعادة بالكامل")]
        Returned = 3
    }
}