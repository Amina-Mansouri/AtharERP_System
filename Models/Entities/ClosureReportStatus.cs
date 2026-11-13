using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ClosureReportStatus
    {
        [Display(Name = "بانتظار الاعتماد")]
        Pending = 1,

        [Display(Name = "معتمد")]
        Approved = 2,

        [Display(Name = "مرفوض")]
        Rejected = 3
    }
}