using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    // التصنيف القطاعي للمشروع (بند P5) — لا علاقة له بالمعنى القديم (رئيسي/فرعي)
    // الذي انتقل إلى ProjectScope.
    public enum ProjectType
    {
        [Display(Name = "حكومي")]
        Governmental = 1,

        [Display(Name = "بلدي")]
        Municipal = 2,

        [Display(Name = "خاص")]
        Private = 3,

        [Display(Name = "استثماري")]
        Investment = 4
    }
}