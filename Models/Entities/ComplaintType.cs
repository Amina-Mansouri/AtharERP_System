using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ComplaintType
    {
        [Display(Name = "زميل عمل")]
        Colleague = 1,

        [Display(Name = "المدير المباشر")]
        DirectManager = 2,

        [Display(Name = "بيئة العمل")]
        WorkEnvironment = 3,

        [Display(Name = "رواتب ومستحقات")]
        Payroll = 4,

        [Display(Name = "أخرى")]
        Other = 5
    }
}