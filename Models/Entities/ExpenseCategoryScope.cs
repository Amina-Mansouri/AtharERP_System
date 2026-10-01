using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ExpenseCategoryScope
    {
        [Display(Name = "مصروف مشروع")]
        Project = 1,

        [Display(Name = "مصروف إداري عام")]
        General = 2
    }
}