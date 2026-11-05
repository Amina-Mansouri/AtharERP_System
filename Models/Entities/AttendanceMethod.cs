using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum AttendanceMethod
    {
        [Display(Name = "بصمة")]
        Fingerprint = 1,

        [Display(Name = "موقع جغرافي")]
        Location = 2
    }
}