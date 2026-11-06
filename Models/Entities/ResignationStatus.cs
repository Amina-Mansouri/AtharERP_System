using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ResignationStatus
    {
        [Display(Name = "بانتظار الاعتماد")]
        Pending = 0,

        [Display(Name = "معتمدة")]
        Approved = 1,

        [Display(Name = "مرفوضة")]
        Rejected = 2
    }
}