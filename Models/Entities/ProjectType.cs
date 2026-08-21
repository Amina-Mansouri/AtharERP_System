using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ProjectType
    {
        [Display(Name = "مشروع رئيسي")]
        Main = 1,

        [Display(Name = "مشروع فرعي")]
        Sub = 2
    }
}