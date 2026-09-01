using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum TaskOutputType
    {
        [Display(Name = "مخطط")]
        Drawing = 1,

        [Display(Name = "تقرير")]
        Report = 2,

        [Display(Name = "جدول كميات")]
        QuantityTable = 3,

        [Display(Name = "عرض تقديمي")]
        Presentation = 4
    }
}