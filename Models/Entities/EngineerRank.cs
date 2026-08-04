using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum EngineerRank
    {
        [Display(Name = "غير محدد")]
        None = 0,

        [Display(Name = "مبتدئ")]
        Junior = 1,

        [Display(Name = "متوسط")]
        MidLevel = 2,

        [Display(Name = "خبير")]
        Senior = 3,

        [Display(Name = "رئيسي")]
        Lead = 4,

        [Display(Name = "استشاري")]
        Principal = 5
    }
}