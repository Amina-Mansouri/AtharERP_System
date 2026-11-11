using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities;

public enum SalaryAdvanceStatus
{
    [Display(Name = "بانتظار اعتماد المسؤول المالي")]
    Pending = 1,

    [Display(Name = "معتمدة")]
    Approved = 2,

    [Display(Name = "مرفوضة")]
    Rejected = 3,

    [Display(Name = "بانتظار اعتماد المدير العام")]
    PendingGeneralManager = 4
}