using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum FileCategory
    {
        [Display(Name = "لوحات أوتوكاد")]
        DG = 1,

        [Display(Name = "صور")]
        PH = 2,

        [Display(Name = "تقرير")]
        RE = 3,

        [Display(Name = "طلب")]
        REQ = 4
    }
}