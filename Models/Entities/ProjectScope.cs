using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    // الاسم السابق: ProjectType (بمعنى "رئيسي/فرعي") — أُعيدت التسمية لتحرير اسم
    // ProjectType للتصنيف القطاعي الجديد (حكومي/بلدي/خاص/استثماري) حسب اختيارك.
    public enum ProjectScope
    {
        [Display(Name = "مشروع رئيسي")]
        Main = 1,

        [Display(Name = "مشروع فرعي")]
        Sub = 2
    }
}