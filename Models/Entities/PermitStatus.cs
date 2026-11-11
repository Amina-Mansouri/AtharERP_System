using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum PermitStatus
    {
        [Display(Name = "بانتظار اعتماد الشؤون الإدارية")]
        Pending = 0,

        [Display(Name = "معتمد")]
        Approved = 1,

        [Display(Name = "مرفوض")]
        Rejected = 2,

        [Display(Name = "بانتظار اعتماد المدير العام")]
        PendingGeneralManager = 3
    }
}