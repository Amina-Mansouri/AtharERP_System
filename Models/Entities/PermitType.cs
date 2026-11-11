using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum PermitType
    {
        [Display(Name = "تأخير عن الدوام")]
        Lateness = 1,

        [Display(Name = "خروج مبكر")]
        EarlyLeave = 2,

        [Display(Name = "استئذان خلال الدوام")]
        MidShiftOut = 3,

        [Display(Name = "غياب لساعات")]
        PartialAbsence = 4
    }
}