using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum LeaveRequestStatus
    {
        [Display(Name = "بانتظار اعتماد الشؤون الإدارية")]
        Pending = 0,

        [Display(Name = "معتمدة")]
        Approved = 1,

        [Display(Name = "مرفوضة")]
        Rejected = 2,

        [Display(Name = "بانتظار اعتماد المدير العام")]
        PendingGeneralManager = 3
    }
}