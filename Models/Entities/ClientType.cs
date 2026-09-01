using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ClientType
    {
        [Display(Name = "جهة حكومية")]
        Government = 1,

        [Display(Name = "شركة")]
        Company = 2,

        [Display(Name = "فرد")]
        Individual = 3
    }
}