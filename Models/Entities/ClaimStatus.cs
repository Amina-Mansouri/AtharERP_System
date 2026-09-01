using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ClaimStatus
    {
        [Display(Name = "معلّقة")]
        Pending = 1,

        [Display(Name = "معتمدة فنياً")]
        TechnicalApproved = 2,

        [Display(Name = "معتمدة")]
        Approved = 3,

        [Display(Name = "مرفوضة")]
        Rejected = 4
    }
}