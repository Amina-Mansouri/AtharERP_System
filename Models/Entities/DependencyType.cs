using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    // القيم الأربع مأخوذة حرفياً من ملاحظة حقل type في جدول TaskDependency (القسم 3.8)
    public enum DependencyType
    {
        [Display(Name = "انتهاء إلى بداية")]
        FinishToStart = 1,

        [Display(Name = "بداية إلى بداية")]
        StartToStart = 2,

        [Display(Name = "انتهاء إلى انتهاء")]
        FinishToFinish = 3,

        [Display(Name = "بداية إلى انتهاء")]
        StartToFinish = 4
    }
}