using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum CareerTrack
    {
        [Display(Name = "المسار الهندسي")]
        Engineering = 1,

        [Display(Name = "المسار المعماري")]
        Architecture = 2,

        [Display(Name = "المسار الإداري")]
        Administrative = 3
    }
}